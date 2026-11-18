import React, { useState, useRef, useEffect } from 'react';
import {
  Play,
  Loader2,
  Search,
  Facebook,
  AlertCircle,
  CheckCircle2,
  Table as TableIcon,
  ExternalLink,
  RefreshCw,
  SlidersHorizontal,
  Key,
  Activity,
  Check
} from 'lucide-react';

// Import file text (Cách này hoạt động tốt với Create React App hoặc Webpack)
// Nếu bạn dùng Vite, hãy đổi thành: import apiFileUrl from './api.txt?url';
import apiTextContent from './api.txt?raw';

// --- Utility Functions ---
const delay = (ms) => new Promise(res => setTimeout(res, ms));

const normalizeData = (rawData) => {
  return rawData.map((item, index) => ({
    id: item.id || index,
    url: item.url || item.postUrl || '#',
    author: item.user?.name || item.author?.name || item.pageName || item.author || 'Tác giả không rõ',
    text: item.text || item.postText || item.content || item.description || '',
    likes: item.likesCount || item.likes || item.reactionsCount || 0,
    comments: item.commentsCount || item.comments || 0,
    shares: item.sharesCount || item.shares || 0,
    date: item.time || item.date || item.createdAt || item.timestamp || 'Không có'
  }));
};

export default function App() {
  // Token States
  const [tokens, setTokens] = useState([]);
  const [activeTokenIndex, setActiveTokenIndex] = useState(0);

  // BIẾN ĐẾM NGẦM
  const [tokenUsageCount, setTokenUsageCount] = useState(0);
  const TOKEN_LIMIT = 9998;
  const FETCH_CHUNK_SIZE = 10;

  // Search Scraper States
  const [keyword, setKeyword] = useState('bất động sản');
  const [strictSearch, setStrictSearch] = useState(false);
  const [filterRecent, setFilterRecent] = useState(false);
  const [timeInterval, setTimeInterval] = useState('Since 24 Hours');
  const [targetItems, setTargetItems] = useState(100);

  const [status, setStatus] = useState('idle');
  const [statusMessage, setStatusMessage] = useState('Đang khởi tạo...');
  const [results, setResults] = useState([]);
  const [errorMsg, setErrorMsg] = useState('');

  const statusRef = useRef(null);

  // TỰ ĐỘNG ĐỌC FILE api.txt KHI COMPONENT MOUNT
 // TỰ ĐỘNG ĐỌC FILE KHI COMPONENT MOUNT BẰNG CÁCH GỌI TRỰC TIẾP
  useEffect(() => {
    try {
      const tokenList = apiTextContent.split('\n')
                            .map(t => t.trim())
                            .filter(t => t.length > 0);
      if (tokenList.length > 0) {
        setTokens(tokenList);
        setStatusMessage(`Đã tự động tải thành công ${tokenList.length} token từ api.txt.`);
      } else {
        setErrorMsg('Tệp api.txt trống hoặc không hợp lệ.');
        setStatus('error');
      }
    } catch (err) {
      console.error("Lỗi đọc file:", err);
      setErrorMsg('Không thể đọc dữ liệu từ api.txt.');
      setStatus('error');
    }
  }, []);

  useEffect(() => {
    if (statusRef.current) {
      statusRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [status, statusMessage]);

  const startCrawling = async () => {
    if (tokens.length === 0) {
      setErrorMsg('Không có token nào được tải. Vui lòng kiểm tra tệp api.txt.');
      setStatus('error');
      return;
    }

    if (!keyword.trim()) {
      setErrorMsg('Vui lòng nhập từ khóa tìm kiếm.');
      setStatus('error');
      return;
    }

    setStatus('running');
    setErrorMsg('');

    let allResults = [...results];
    let remainingToScrape = targetItems;
    let currentTokenIdx = activeTokenIndex;
    let currentTokenUsage = tokenUsageCount;

    try {
      while (remainingToScrape > 0 && currentTokenIdx < tokens.length) {

        if (currentTokenUsage >= TOKEN_LIMIT) {
          currentTokenIdx++;
          currentTokenUsage = 0;
          setActiveTokenIndex(currentTokenIdx);
          setTokenUsageCount(0);

          if (currentTokenIdx >= tokens.length) {
            setStatusMessage('Đã sử dụng hết tất cả Token trong danh sách.');
            break;
          }
          setStatusMessage(`Đã đạt giới hạn 9998. Tự động chuyển sang Token #${currentTokenIdx + 1}...`);
          await delay(2000);
          continue;
        }

        const currentToken = tokens[currentTokenIdx];

        const amountToFetchThisRun = Math.min(
          FETCH_CHUNK_SIZE,
          remainingToScrape,
          TOKEN_LIMIT - currentTokenUsage
        );

        setStatusMessage(`Token #${currentTokenIdx + 1} (Đã dùng: ${currentTokenUsage}/${TOKEN_LIMIT}) | Đang cào thêm ${amountToFetchThisRun} bài...`);

        const actorId = 'alien_force~facebook-scraper-pro-rental';
        const startRunUrl = `https://api.apify.com/v2/acts/${actorId}/runs?token=${currentToken}`;

        const startPayload = {
          scraperMode: "facebookSearchScraper",
          keyword: keyword.trim(),
          enableStrictSearch: strictSearch,
          filterByRecent: filterRecent,
          fetchPostsInTimeInterval: timeInterval,
          resultsLimit: amountToFetchThisRun,
          maxItems: amountToFetchThisRun,
          maxResults: amountToFetchThisRun
        };

        const startRes = await fetch(startRunUrl, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(startPayload)
        });

        if (!startRes.ok) {
          const errData = await startRes.json();
          throw new Error(`Lỗi Apify (Token #${currentTokenIdx + 1}): ${errData.error?.message || startRes.status}`);
        }

        const startData = await startRes.json();
        const runId = startData.data.id;

        let runStatus = 'RUNNING';
        let defaultDatasetId = null;

        while (runStatus === 'RUNNING' || runStatus === 'READY') {
          await delay(3000);
          const checkUrl = `https://api.apify.com/v2/actor-runs/${runId}?token=${currentToken}`;
          const checkRes = await fetch(checkUrl);

          if (checkRes.ok) {
            const checkData = await checkRes.json();
            runStatus = checkData.data.status;
            defaultDatasetId = checkData.data.defaultDatasetId;
          } else {
            throw new Error('Lỗi mạng khi kiểm tra trạng thái.');
          }
        }

        if (runStatus === 'SUCCEEDED' && defaultDatasetId) {
          const datasetUrl = `https://api.apify.com/v2/datasets/${defaultDatasetId}/items?token=${currentToken}`;
          const datasetRes = await fetch(datasetUrl);
          const datasetData = await datasetRes.json();

          const normalized = normalizeData(datasetData);

          if (normalized.length === 0) {
            setStatusMessage('Không tìm thấy thêm bài viết nào cho từ khóa này. Dừng chương trình.');
            break;
          }

          allResults = [...allResults, ...normalized];
          setResults(allResults);

          remainingToScrape -= normalized.length;
          currentTokenUsage += normalized.length;

          setTokenUsageCount(currentTokenUsage);

          if (remainingToScrape > 0) {
            await delay(2000);
          }
        } else {
          throw new Error(`Tiến trình cào thất bại với trạng thái: ${runStatus}`);
        }
      }

      setStatus('success');
      setStatusMessage(`Hoàn tất! Đã thu thập được tổng cộng ${allResults.length} bản ghi.`);

    } catch (err) {
      console.error(err);
      setStatus('error');
      setErrorMsg(err.message || 'Đã xảy ra lỗi không mong muốn.');
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 font-sans p-4 md:p-8">
      <div className="max-w-6xl mx-auto space-y-6">

        <header className="flex items-center space-x-3 pb-6 border-b border-slate-200">
          <div className="bg-blue-600 p-3 rounded-xl shadow-lg shadow-blue-600/20">
            <Facebook className="w-8 h-8 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-slate-800">Cào Dữ Liệu Facebook (Auto Token)</h1>
            <p className="text-slate-500 text-sm">Tự động lấy token từ file local và chạy vòng lặp</p>
          </div>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-1 space-y-6">

            {/* API Configuration (Read-only since it's auto-loaded) */}
            <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200">
              <h2 className="text-lg font-semibold mb-4 flex items-center">
                <Key className="w-5 h-5 mr-2 text-slate-400" /> Trạng thái API Token
              </h2>
              <div className="space-y-4">
                {tokens.length > 0 ? (
                  <div className="text-sm space-y-3">
                    <div className="flex items-center text-green-700 bg-green-50 px-4 py-3 rounded-xl border border-green-200">
                      <Check className="w-5 h-5 mr-2 text-green-500" />
                      <div>
                        <p className="font-semibold">Đã nạp file api.txt</p>
                        <p className="text-xs mt-0.5 opacity-90">Tổng cộng: {tokens.length} tokens</p>
                      </div>
                    </div>

                    <div className="text-blue-700 bg-blue-50 px-4 py-3 rounded-xl border border-blue-200">
                      <p className="font-semibold flex items-center mb-1">
                        <Activity className="w-4 h-4 mr-1.5"/> Đang sử dụng Token #{activeTokenIndex + 1}
                      </p>
                      <div className="w-full bg-blue-200 rounded-full h-2 mt-2 mb-1">
                        <div className="bg-blue-600 h-2 rounded-full transition-all duration-500" style={{ width: `${(tokenUsageCount / TOKEN_LIMIT) * 100}%` }}></div>
                      </div>
                      <p className="text-xs text-right opacity-80">{tokenUsageCount} / 9998 lượt</p>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center text-amber-700 bg-amber-50 px-4 py-3 rounded-xl border border-amber-200">
                    <Loader2 className="w-5 h-5 mr-2 text-amber-500 animate-spin" />
                    <span className="text-sm font-medium">Đang tìm đọc tệp api.txt...</span>
                  </div>
                )}
              </div>
            </div>

            {/* Search Configuration */}
            <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200">
              <h2 className="text-lg font-semibold mb-4 flex items-center">
                <SlidersHorizontal className="w-5 h-5 mr-2 text-slate-400" /> Cấu hình Tìm Kiếm
              </h2>

              <div className="space-y-5">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1 flex items-center">
                    <Search className="w-4 h-4 mr-1 text-slate-400" /> Từ khóa (Keyword)
                  </label>
                  <input
                    type="text"
                    value={keyword}
                    onChange={(e) => setKeyword(e.target.value)}
                    placeholder="Nhập từ khóa..."
                    className="w-full px-4 py-2 bg-slate-50 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                    disabled={status === 'running'}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1 flex items-center">
                    <TableIcon className="w-4 h-4 mr-1 text-slate-400" /> Mục tiêu cào tổng cộng
                  </label>
                  <input
                    type="number"
                    value={targetItems}
                    onChange={(e) => setTargetItems(Number(e.target.value))}
                    className="w-full px-4 py-2 bg-slate-50 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                    disabled={status === 'running'}
                  />
                </div>

                <div className="pt-2">
                  <button
                    onClick={startCrawling}
                    disabled={status === 'running' || tokens.length === 0}
                    className={`w-full py-3 px-4 rounded-lg font-medium flex items-center justify-center transition-all shadow-sm
                      ${status === 'running' || tokens.length === 0
                        ? 'bg-slate-100 text-slate-400 cursor-not-allowed'
                        : 'bg-blue-600 text-white hover:bg-blue-700 active:scale-[0.98]'}`}
                  >
                    {status === 'running' ? (
                      <><Loader2 className="w-5 h-5 mr-2 animate-spin" /> Đang thu thập...</>
                    ) : (
                      <><Play className="w-5 h-5 mr-2" /> Bắt Đầu Chạy</>
                    )}
                  </button>
                </div>
              </div>
            </div>

            {/* Status Panel */}
            {(status !== 'idle' || errorMsg) && (
              <div ref={statusRef} className={`p-4 rounded-xl border ${
                  status === 'error' ? 'bg-red-50 border-red-200' :
                  status === 'success' ? 'bg-green-50 border-green-200' :
                  'bg-blue-50 border-blue-200'
                }`}>
                <div className="flex items-start">
                  {status === 'error' && <AlertCircle className="w-5 h-5 text-red-500 mr-2 mt-0.5 flex-shrink-0" />}
                  {status === 'success' && <CheckCircle2 className="w-5 h-5 text-green-500 mr-2 mt-0.5 flex-shrink-0" />}
                  {status === 'running' && <RefreshCw className="w-5 h-5 text-blue-500 mr-2 mt-0.5 flex-shrink-0 animate-spin" />}

                  <div>
                    <h3 className={`font-semibold text-sm ${status === 'error' ? 'text-red-800' : status === 'success' ? 'text-green-800' : 'text-blue-800'}`}>
                      {status === 'error' ? 'Lỗi' : status === 'success' ? 'Hoàn tất' : 'Đang xử lý ngầm'}
                    </h3>
                    <p className={`text-sm mt-1 ${status === 'error' ? 'text-red-600' : status === 'success' ? 'text-green-600' : 'text-blue-600'}`}>
                      {errorMsg || statusMessage}
                    </p>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Results Panel */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 overflow-hidden h-full flex flex-col">
              <div className="p-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
                <h2 className="text-lg font-semibold flex items-center">
                  <TableIcon className="w-5 h-5 mr-2 text-slate-400" /> Dữ Liệu Thu Thập Được
                </h2>
                <span className="text-xs font-medium bg-slate-200 text-slate-600 px-2 py-1 rounded-full">
                  {results.length} bản ghi
                </span>
              </div>

              <div className="p-0 overflow-auto flex-grow" style={{ minHeight: '400px', maxHeight: '800px' }}>
                {results.length === 0 ? (
                  <div className="h-full flex flex-col items-center justify-center text-slate-400 p-8 text-center">
                    <TableIcon className="w-12 h-12 mb-3 opacity-20" />
                    <p>Chưa có dữ liệu nào được trích xuất.</p>
                  </div>
                ) : (
                  <table className="w-full text-left border-collapse min-w-[800px]">
                    <thead>
                      <tr className="bg-slate-50 text-slate-500 text-xs uppercase tracking-wider border-b border-slate-200">
                        <th className="p-4 font-semibold w-1/4">Tác giả & Thời gian</th>
                        <th className="p-4 font-semibold w-2/4">Tóm tắt nội dung</th>
                        <th className="p-4 font-semibold w-1/4">Tương tác</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {results.map((row, idx) => (
                        <tr key={idx} className="hover:bg-slate-50/80 transition-colors group">
                          <td className="p-4 align-top">
                            <div className="font-medium text-slate-800 break-words">{row.author}</div>
                          </td>
                          <td className="p-4 align-top">
                            <div className="text-sm text-slate-700 line-clamp-3 mb-2">{row.text || 'Không có văn bản'}</div>
                            <a href={row.url} target="_blank" rel="noopener noreferrer" className="text-xs text-blue-600 hover:text-blue-800 flex items-center">
                              Xem bài gốc <ExternalLink className="w-3 h-3 ml-1" />
                            </a>
                          </td>
                          <td className="p-4 align-top">
                            <div className="grid grid-cols-2 gap-2 text-xs">
                              <div className="bg-slate-100 px-2 py-1 rounded">
                                <span className="block text-slate-400 uppercase text-[10px]">Thích</span>
                                <span className="font-semibold">{row.likes}</span>
                              </div>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}