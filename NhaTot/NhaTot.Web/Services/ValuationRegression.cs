namespace NhaTot.Web.Services;

public sealed record ValuationSample(string Key, int Type, int District, int Ward, double Area, double Price, DateTime Date);
public sealed record RegressionMetrics(double Mae, double Rmse, double R2, double Mape, double BaselineMae, int Count);
public sealed class ValuationArtifact
{
    public int Schema { get; set; } = 1;
    public DateTime Origin { get; set; }
    public DateTime LastDate { get; set; }
    public string[] Categories { get; set; } = [];
    public double[] Weights { get; set; } = [];
    public ValuationSample[] Support { get; set; } = [];
    public double LogError90 { get; set; }
    public RegressionMetrics Metrics { get; set; } = null!;
    public bool Accepted => Metrics != null && Metrics.Mae < Metrics.BaselineMae && Metrics.Mape <= .35;

    public double[] Features(ValuationSample row)
    {
        var values = new double[4 + Categories.Length];
        values[0] = 1; values[1] = Math.Log(row.Area); values[2] = (row.Date - Origin).TotalDays / 365.25;
        values[3] = Math.Log(row.Area) * Math.Log(row.Area) / 10;
        var keys = new[] { "type:" + row.Type, "district:" + row.District, "ward:" + row.Ward };
        for (var i = 0; i < Categories.Length; i++) values[4 + i] = keys.Contains(Categories[i]) ? 1 : 0;
        return values;
    }
    public double Predict(ValuationSample row) => Math.Exp(Features(row).Zip(Weights, (x, w) => x * w).Sum());
    public bool Supports(ValuationSample row) => row.Area > 0 && Support.Count(x => x.Type == row.Type && x.District == row.District) >= 5
        && row.Area >= Support.Where(x => x.Type == row.Type && x.District == row.District).Min(x => x.Area) * .8
        && row.Area <= Support.Where(x => x.Type == row.Type && x.District == row.District).Max(x => x.Area) * 1.2;
}

public static class ValuationRegression
{
    public static ValuationSample[] Clean(IEnumerable<ValuationSample> source) => source
        .Where(x => x.Type > 0 && x.District > 0 && x.Ward > 0 && x.Area is >= 1 and <= 100000
            && x.Price is >= 1000000 and <= 1000000000000 && double.IsFinite(x.Area) && double.IsFinite(x.Price)
            && x.Price / x.Area is >= 10000 and <= 2000000000 && x.Date.Year >= 2000 && x.Date <= DateTime.UtcNow.AddDays(1))
        .GroupBy(x => x.Key).Select(g => g.OrderByDescending(x => x.Date).First())
        .GroupBy(x => new { x.Type, x.Ward, Area = Math.Round(x.Area, 1), Price = Math.Round(x.Price, -0) })
        .Select(g => g.OrderByDescending(x => x.Date).First()).OrderBy(x => x.Date).ThenBy(x => x.Key).ToArray();

    public static ValuationArtifact Train(IEnumerable<ValuationSample> source)
    {
        var rows = Clean(source);
        if (rows.Length < 60) throw new InvalidOperationException($"Có {rows.Length} mẫu hợp lệ; cần ít nhất 60 mẫu độc lập để tách huấn luyện/hiệu chỉnh/kiểm tra.");
        // Tách theo ngày nguyên vẹn: không để cùng ngày xuất hiện ở hai tập.
        var trainBoundary = rows[(int)(rows.Length * .6)].Date.Date;
        var testBoundary = rows[(int)(rows.Length * .8)].Date.Date;
        var train = rows.Where(x => x.Date.Date < trainBoundary).ToArray();
        var calibration = rows.Where(x => x.Date.Date >= trainBoundary && x.Date.Date < testBoundary).ToArray();
        var test = rows.Where(x => x.Date.Date >= testBoundary).ToArray();
        if (train.Length < 30 || calibration.Length < 10 || test.Length < 10)
            throw new InvalidOperationException("Dữ liệu chưa trải đủ thời gian để tách tập đánh giá độc lập (30/10/10).");
        var model = new ValuationArtifact {
            Origin = train[0].Date.Date, LastDate = rows[^1].Date, Support = train,
            Categories = train.SelectMany(x => new[] { "type:" + x.Type, "district:" + x.District, "ward:" + x.Ward }).Distinct().Order().ToArray()
        };
        if (model.Categories.Length > 300) throw new InvalidOperationException("Quá nhiều địa phương cho mô hình đầu tiên; cần huấn luyện riêng từng khu vực.");
        var n = 4 + model.Categories.Length;
        var matrix = new double[n, n]; var rhs = new double[n];
        foreach (var row in train)
        {
            var f = model.Features(row); var y = Math.Log(row.Price);
            for (var i = 0; i < n; i++) { rhs[i] += f[i] * y; for (var j = 0; j < n; j++) matrix[i,j] += f[i] * f[j]; }
        }
        for (var i = 1; i < n; i++) matrix[i,i] += 1; // L2, không phạt intercept.
        model.Weights = Solve(matrix, rhs);
        var residuals = calibration.Select(x => Math.Abs(Math.Log(x.Price) - Math.Log(model.Predict(x)))).Order().ToArray();
        model.LogError90 = residuals[Math.Min(residuals.Length - 1, (int)Math.Ceiling((residuals.Length + 1) * .9) - 1)];
        var unitMedian = Median(train.Select(x => x.Price / x.Area));
        var errors = test.Select(x => model.Predict(x) - x.Price).ToArray();
        var mean = test.Average(x => x.Price); var variance = test.Sum(x => Math.Pow(x.Price - mean, 2));
        model.Metrics = new(errors.Average(Math.Abs), Math.Sqrt(errors.Average(x => x*x)),
            variance > 0 ? 1 - errors.Sum(x => x*x) / variance : 0,
            test.Select((x,i) => Math.Abs(errors[i]) / x.Price).Average(),
            test.Average(x => Math.Abs(unitMedian * x.Area - x.Price)), test.Length);
        return model;
    }
    public static double Median(IEnumerable<double> source) { var values = source.Order().ToArray(); return values.Length % 2 == 0 ? (values[values.Length/2-1]+values[values.Length/2])/2 : values[values.Length/2]; }
    private static double[] Solve(double[,] a, double[] b)
    {
        var n = b.Length;
        for (var k = 0; k < n; k++)
        {
            var pivot = k; for (var i = k+1; i < n; i++) if (Math.Abs(a[i,k]) > Math.Abs(a[pivot,k])) pivot = i;
            if (Math.Abs(a[pivot,k]) < 1e-12) throw new InvalidOperationException("Dữ liệu không đủ đa dạng để huấn luyện.");
            for (var j = k; j < n; j++) (a[k,j],a[pivot,j]) = (a[pivot,j],a[k,j]);
            (b[k],b[pivot]) = (b[pivot],b[k]);
            var d = a[k,k]; for (var j = k; j < n; j++) a[k,j] /= d; b[k] /= d;
            for (var i = 0; i < n; i++) if (i != k) { var f = a[i,k]; for (var j = k; j < n; j++) a[i,j] -= f*a[k,j]; b[i] -= f*b[k]; }
        }
        return b;
    }
}
