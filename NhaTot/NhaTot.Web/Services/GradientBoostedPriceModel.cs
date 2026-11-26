namespace NhaTot.Web.Services;

public sealed class GbtNode
{
    public bool IsLeaf { get; set; }
    public int FeatureIndex { get; set; }
    public double Threshold { get; set; }
    public double LeafValue { get; set; }
    public GbtNode? Left { get; set; }
    public GbtNode? Right { get; set; }
}

public sealed class GbtArtifact
{
    public const string DefaultModelName = "GBDT-XGBoostStyle-v1";

    public string ModelName { get; set; } = DefaultModelName;
    public DateTime Origin { get; set; }
    public DateTime LastDate { get; set; }
    public double BaseScore { get; set; }
    public double LearningRate { get; set; } = 0.12;
    public double RegLambda { get; set; } = 2.0;
    public double Gamma { get; set; } = 0.01;
    public int MaxDepth { get; set; } = 4;
    public int SampleCount { get; set; }
    public int TrainCount { get; set; }
    public int TestCount { get; set; }
    public double GlobalLogUnitMean { get; set; }
    public Dictionary<int, double> TypeMeans { get; set; } = new();
    public Dictionary<int, double> DistrictMeans { get; set; } = new();
    public Dictionary<int, double> WardMeans { get; set; } = new();
    public List<GbtNode> Trees { get; set; } = new();
    public RegressionMetrics Metrics { get; set; } = null!;

    public double[] ExtractFeatures(ValuationSample s)
    {
        var logArea = Math.Log(Math.Max(1.0, s.Area));
        var years = (s.Date - Origin).TotalDays / 365.25;
        var typeEnc = TypeMeans.GetValueOrDefault(s.Type, GlobalLogUnitMean);
        var distEnc = DistrictMeans.GetValueOrDefault(s.District, GlobalLogUnitMean);
        var wardEnc = WardMeans.GetValueOrDefault(s.Ward, distEnc);
        return [logArea, s.Area, years, typeEnc, distEnc, wardEnc];
    }

    public double Predict(ValuationSample s)
    {
        var f = ExtractFeatures(s);
        var sum = BaseScore;
        foreach (var tree in Trees)
            sum += LearningRate * EvaluateTree(tree, f);
        return Math.Exp(sum);
    }

    private static double EvaluateTree(GbtNode node, double[] f)
    {
        var cur = node;
        while (!cur.IsLeaf)
        {
            cur = f[cur.FeatureIndex] <= cur.Threshold ? cur.Left! : cur.Right!;
        }
        return cur.LeafValue;
    }
}

/// <summary>
/// Pure-C# Gradient Boosted Decision Tree (XGBoost-style second-order split gain &amp; L2 leaf regularization)
/// for real-estate market price estimation and moderation anomaly detection.
/// </summary>
public static class GradientBoostedPriceModel
{
    public const int DefaultNumTrees = 35;
    public const int DefaultMaxDepth = 4;
    public const double DefaultLearningRate = 0.12;
    public const double DefaultRegLambda = 2.0;
    public const double DefaultGamma = 0.01;
    public const int MinLeafSamples = 5;

    public static GbtArtifact Train(
        IEnumerable<ValuationSample> source,
        int numTrees = DefaultNumTrees,
        int maxDepth = DefaultMaxDepth,
        double learningRate = DefaultLearningRate,
        double regLambda = DefaultRegLambda,
        double gamma = DefaultGamma)
    {
        var rows = ValuationRegression.Clean(source);
        if (rows.Length < 60)
            throw new InvalidOperationException($"Có {rows.Length} mẫu hợp lệ; cần ít nhất 60 mẫu để huấn luyện GBDT.");

        var splitIdx = Math.Clamp((int)(rows.Length * 0.8), 40, rows.Length - 10);
        var splitDate = rows[splitIdx].Date.Date;
        var train = rows.Where(x => x.Date.Date < splitDate).ToArray();
        var test = rows.Where(x => x.Date.Date >= splitDate).ToArray();
        if (train.Length < 40 || test.Length < 10)
        {
            train = rows[..splitIdx];
            test = rows[splitIdx..];
        }

        var globalLogUnitMean = train.Average(x => Math.Log(x.Price / x.Area));
        static Dictionary<int, double> SmoothedEnc(ValuationSample[] src, Func<ValuationSample, int> keySelector, double prior)
        {
            const double priorWeight = 5.0;
            return src.GroupBy(keySelector).ToDictionary(
                g => g.Key,
                g => (g.Sum(x => Math.Log(x.Price / x.Area)) + priorWeight * prior) / (g.Count() + priorWeight));
        }

        var artifact = new GbtArtifact
        {
            Origin = train[0].Date.Date,
            LastDate = rows[^1].Date,
            LearningRate = learningRate,
            RegLambda = regLambda,
            Gamma = gamma,
            MaxDepth = maxDepth,
            SampleCount = rows.Length,
            TrainCount = train.Length,
            TestCount = test.Length,
            GlobalLogUnitMean = globalLogUnitMean,
            TypeMeans = SmoothedEnc(train, x => x.Type, globalLogUnitMean),
            DistrictMeans = SmoothedEnc(train, x => x.District, globalLogUnitMean),
            WardMeans = SmoothedEnc(train, x => x.Ward, globalLogUnitMean)
        };

        var y = train.Select(x => Math.Log(x.Price)).ToArray();
        artifact.BaseScore = y.Average();

        var xTrain = train.Select(artifact.ExtractFeatures).ToArray();
        var preds = Enumerable.Repeat(artifact.BaseScore, train.Length).ToArray();
        var allIndices = Enumerable.Range(0, train.Length).ToArray();

        for (var t = 0; t < numTrees; t++)
        {
            // Squared error loss: L = 0.5 * (pred - y)^2 -> g_i = pred_i - y_i, h_i = 1
            var grads = new double[train.Length];
            for (var i = 0; i < train.Length; i++)
                grads[i] = preds[i] - y[i];

            var tree = BuildTree(xTrain, grads, allIndices, 0, maxDepth, regLambda, gamma);
            artifact.Trees.Add(tree);

            for (var i = 0; i < train.Length; i++)
                preds[i] += learningRate * PredictSingle(tree, xTrain[i]);
        }

        // Evaluate on holdout test set
        var unitMedian = ValuationRegression.Median(train.Select(x => x.Price / x.Area));
        var actual = test.Select(x => x.Price).ToArray();
        var predTest = test.Select(artifact.Predict).ToArray();
        var baseTest = test.Select(x => unitMedian * x.Area).ToArray();
        var meanActual = actual.Average();

        double mae = 0, sqErr = 0, mape = 0, baseMae = 0, ssTot = 0;
        for (var i = 0; i < test.Length; i++)
        {
            var err = Math.Abs(actual[i] - predTest[i]);
            mae += err;
            sqErr += err * err;
            mape += err / actual[i];
            baseMae += Math.Abs(actual[i] - baseTest[i]);
            ssTot += (actual[i] - meanActual) * (actual[i] - meanActual);
        }

        mae /= test.Length;
        var rmse = Math.Sqrt(sqErr / test.Length);
        mape /= test.Length;
        baseMae /= test.Length;
        var r2 = ssTot > 0 ? 1.0 - sqErr / ssTot : 0.0;

        artifact.Metrics = new RegressionMetrics(mae, rmse, r2, mape, baseMae, test.Length);
        return artifact;
    }

    private static double PredictSingle(GbtNode node, double[] f)
    {
        var cur = node;
        while (!cur.IsLeaf)
            cur = f[cur.FeatureIndex] <= cur.Threshold ? cur.Left! : cur.Right!;
        return cur.LeafValue;
    }

    private static GbtNode BuildTree(
        double[][] x,
        double[] g,
        int[] indices,
        int depth,
        int maxDepth,
        double regLambda,
        double gamma)
    {
        double sumG = 0;
        foreach (var idx in indices) sumG += g[idx];
        double countH = indices.Length;
        double leafWeight = -sumG / (countH + regLambda);

        if (depth >= maxDepth || indices.Length < MinLeafSamples * 2)
            return new GbtNode { IsLeaf = true, LeafValue = leafWeight };

        double parentScore = (sumG * sumG) / (countH + regLambda);
        double bestGain = 0;
        int bestFeat = -1;
        double bestThresh = 0;

        int numFeatures = x[0].Length;
        for (var f = 0; f < numFeatures; f++)
        {
            var pairs = new (double Val, double Grad)[indices.Length];
            for (var k = 0; k < indices.Length; k++)
            {
                var idx = indices[k];
                pairs[k] = (x[idx][f], g[idx]);
            }
            Array.Sort(pairs, (a, b) => a.Val.CompareTo(b.Val));

            // Candidate split thresholds (up to 16 quantile boundaries for speed & regularization)
            const int maxBuckets = 16;
            int step = Math.Max(1, pairs.Length / maxBuckets);
            double leftG = 0;
            int leftCount = 0;

            for (var k = 0; k < pairs.Length - 1; k++)
            {
                leftG += pairs[k].Grad;
                leftCount++;

                if (pairs[k].Val == pairs[k + 1].Val) continue;
                if (leftCount < MinLeafSamples || (pairs.Length - leftCount) < MinLeafSamples) continue;
                if (step > 1 && (k % step) != 0 && k < pairs.Length - MinLeafSamples - 1) continue;

                double rightG = sumG - leftG;
                double rightCount = countH - leftCount;

                double gain = 0.5 * (
                    (leftG * leftG) / (leftCount + regLambda) +
                    (rightG * rightG) / (rightCount + regLambda) -
                    parentScore) - gamma;

                if (gain > bestGain)
                {
                    bestGain = gain;
                    bestFeat = f;
                    bestThresh = 0.5 * (pairs[k].Val + pairs[k + 1].Val);
                }
            }
        }

        if (bestFeat < 0)
            return new GbtNode { IsLeaf = true, LeafValue = leafWeight };

        var leftList = new List<int>(indices.Length / 2);
        var rightList = new List<int>(indices.Length / 2);
        foreach (var idx in indices)
        {
            if (x[idx][bestFeat] <= bestThresh) leftList.Add(idx);
            else rightList.Add(idx);
        }

        if (leftList.Count < MinLeafSamples || rightList.Count < MinLeafSamples)
            return new GbtNode { IsLeaf = true, LeafValue = leafWeight };

        return new GbtNode
        {
            IsLeaf = false,
            FeatureIndex = bestFeat,
            Threshold = bestThresh,
            Left = BuildTree(x, g, leftList.ToArray(), depth + 1, maxDepth, regLambda, gamma),
            Right = BuildTree(x, g, rightList.ToArray(), depth + 1, maxDepth, regLambda, gamma)
        };
    }
}
