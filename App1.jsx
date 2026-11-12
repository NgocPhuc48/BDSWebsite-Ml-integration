import React, { useState, useRef, useEffect } from 'react';
import { 
  Play, Loader2, Search, Globe, AlertCircle, CheckCircle2, 
  Table as TableIcon, ExternalLink, RefreshCw, SlidersHorizontal, 
  Key, Activity, Check, Clock, FileSpreadsheet
} from 'lucide-react';

import apiTextContent from './api.txt?raw';

const delay = (ms) => new Promise(res => setTimeout(res, ms));

const COLUMNS = [
  { key: 'postId', label: 'ID Bài viết' },
  { key: 'actionId', label: 'ID Hành động' },
  { key: 'text', label: 'Nội dung văn bản' },
  { key: 'postUrl', label: 'Đường dẫn bài viết' },
  { key: 'createTime', label: 'Thời gian tạo' },
  { key: 'author', label: 'Tên tác giả' },
  { key: 'authorUsername', label: 'Tên người dùng' },
  { key: 'authorUserId', label: 'ID người dùng' },
  { key: 'authorProfileUrl', label: 'Trang cá nhân' },
  { key: 'authorAvatar', label: 'Ảnh đại diện' },
  { key: 'likes', label: 'Lượt thích' },
  { key: 'views', label: 'Lượt xem' },
  { key: 'shares', label: 'Lượt chia sẻ' },
  { key: 'comments', label: 'Bình luận' },
  { key: 'type', label: 'Loại bài' },
  { key: 'imageList', label: 'Danh sách ảnh' },
  { key: 'videoList', label: 'Danh sách video' },
  { key: 'videoCoverImage', label: 'Ảnh bìa video' },
  { key: 'durationInSec', label: 'Thời lượng video (s)' }
];

// Hàm tự chuẩn hóa thời gian sang định dạng dễ đọc
const formatDateTime = (rawTime) => {
  if (!rawTime || rawTime === 'Không có') return 'Không có';
  try {
    // Nếu thời gian là dạng số (timestamp giây hoặc mili-giây)
    let date;
    if (typeof rawTime === 'number' || /^\d+$/.test(rawTime)) {
      const num = Number(rawTime);
      date = new Date(num < 10000000000 ? num * 1000 : num);
    } else {
      // Dạng chuỗi thời gian thông thường
      date = new Date(rawTime);
    }

    if (!isNaN(date.getTime())) {
      // Format sang DD/MM/YYYY HH:mm:ss theo giờ địa phương
      return date.toLocaleString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: false
      });
    }
  } catch (e) {
    console.error("Lỗi định dạng thời gian:", e);
  }
  // Fallback nếu không parse được thì làm sạch chuỗi UTC
  return String(rawTime).replace(' UTC', '').replace('T', ' ').replace('Z', '');
};

const normalizeData = (rawData) => {
  if (!Array.isArray(rawData)) return [];
  return rawData.map((item, index) => {
    const rawTime = item.createTime || item.time || item.date || item.createdAt || item.timestamp || item.created_time || item.postTime || 'Không có';
    return {
      postId: item.postId || item.id || item.post_id || `post_${index}`,
      actionId: item.actionId || item.action_id || '',
      text: item.text || item.postText || item.content || item.description || item.message || '',
      postUrl: item.postUrl || item.url || item.link || item.webUrl || item.post_url || '#',
      createTime: formatDateTime(rawTime), // Tự động chuẩn hóa thời gian ở đây
      author: item.author || item.user?.name || item.pageName || item.authorName || 'Tác giả không rõ',
      authorUsername: item.author_username || item.authorUsername || item.username || '',
      authorUserId: item.author_user_id || item.authorUserId || item.userId || '',
      authorProfileUrl: item.author_profile_url || item.authorProfileUrl || item.userProfileUrl || '#',
      authorAvatar: item.author_avatar || item.authorAvatar || item.userAvatar || '',
      likes: item.like_count || item.likesCount || item.likes || item.reactionsCount || item.reaction_count || 0,
      views: item.view_count || item.viewsCount || item.views || 0,
      shares: item.share_count || item.sharesCount || item.shares || 0,
      comments: item.comment_count || item.commentsCount || item.comments || 0,
      type: item.type || item.postType || 'post',
      imageList: item.image_list || item.images || item.photos || [],
      videoList: item.video_list || item.videos || [],
      videoCoverImage: item.video_cover_image || item.videoCover || '',
      durationInSec: item.durationInSec || item.duration || 0
    };
  });
};

export default function App() {
  const [tokens, setTokens] = useState([]);
  const [activeTokenIndex, setActiveTokenIndex] = useState(0);

  const [keyword, setKeyword] = useState('');
  const [strictSearch, setStrictSearch] = useState(true);
  const [filterRecent, setFilterRecent] = useState(true);
  const [timeInterval, setTimeInterval] = useState('Since 24 Hours');
  const [targetItems] = useState(10); 

  const [status, setStatus] = useState('idle'); 
  const [statusMessage, setStatusMessage] = useState('Đang khởi tạo...');
  const [results, setResults] = useState([]);
  const [errorMsg, setErrorMsg] = useState('');

  const statusRef = useRef(null);

  useEffect(() => {
    try {
      const tokenList = apiTextContent.split('\n')
                            .map(t => t.trim())
                            .filter(t => t.length > 0);
      if (tokenList.length > 0) {
        setTokens(tokenList);
        setStatusMessage(`Đã tự động tải ${tokenList.length} token từ api.txt.`);
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

  const exportToExcel = () => {
    if (results.length === 0) return;

    const headers = COLUMNS.map(col => `"${col.label}"`).join(',');
    const rows = results.map(row => {
      return COLUMNS.map(col => {
        let val = row[col.key];
        if (Array.isArray(val)) {
          val = val.length > 0 ? `${val.length} tệp` : '0';
        }
        const stringVal = val !== undefined && val !== null ? String(val).replace(/"/g, '""') : '';
        return `"${stringVal}"`;
      }).join(',');
    });

    const csvContent = "\uFEFF" + [headers, ...rows].join("\n");
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    
    const link = document.createElement("a");
    link.setAttribute("href", url);
    link.setAttribute("download", `DuLieu_${keyword.replace(/\s+/g, '_')}_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const startCrawling = async (btnKeyword) => {
    if (tokens.length === 0) {
      setErrorMsg('Không có token nào được tải. Vui lòng kiểm tra tệp api.txt.');
      setStatus('error');
      return;
    }
    
    if (!btnKeyword || typeof btnKeyword !== 'string' || !btnKeyword.trim()) {
      setErrorMsg('Vui lòng chọn một từ khóa từ các nút bấm bên dưới.');
      setStatus('error');
      return;
    }

    const searchKeyword = btnKeyword.trim();
    setKeyword(searchKeyword); 
    setStatus('running');
    setErrorMsg('');
    setStatusMessage(`Đang cào ${targetItems} bài viết cho từ khóa "${searchKeyword}" trong 1 lần gọi...`);

    let currentTokenIdx = activeTokenIndex;
    let success = false;
    let finalDatasetItems = [];

    while (currentTokenIdx < tokens.length && !success) {
      const currentToken = tokens[currentTokenIdx];
      setActiveTokenIndex(currentTokenIdx);

      try {
        const actorId = 'alien_force~facebook-scraper-pro-rental';
        const startRunUrl = `https://api.apify.com/v2/acts/${actorId}/runs?token=${currentToken}`;
        
        const startPayload = {
          scraperMode: "facebookSearchScraper",
          keyword: searchKeyword, 
          enableStrictSearch: strictSearch,
          filterByRecent: filterRecent,
          fetchPostsInTimeInterval: timeInterval,
          resultsLimit: targetItems,
          maxItems: targetItems,
          maxResults: targetItems,
          function: "facebook_search_scraper" 
        };

        const startRes = await fetch(startRunUrl, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(startPayload)
        });

        if (!startRes.ok) {
          const errData = await startRes.json();
          throw new Error(`Token lỗi hoặc hết hạn: ${errData.error?.message || startRes.status}`);
        }

        const startData = await startRes.json();
        const runId = startData.data.id;

        let runStatus = 'RUNNING';
        let defaultDatasetId = null;

        while (runStatus === 'RUNNING' || runStatus === 'READY') {
          await delay(4000); 
          const checkUrl = `https://api.apify.com/v2/actor-runs/${runId}?token=${currentToken}`;
          const checkRes = await fetch(checkUrl);
          
          if (checkRes.ok) {
            const checkData = await checkRes.json();
            runStatus = checkData.data.status;
            defaultDatasetId = checkData.data.defaultDatasetId;
            setStatusMessage(`Trạng thái tiến trình: ${runStatus}...`);
          } else {
            throw new Error('Lỗi mạng khi kiểm tra trạng thái tiến trình.');
          }
        }

        if (runStatus === 'SUCCEEDED' && defaultDatasetId) {
          setStatusMessage('Đang tải dữ liệu kết quả...');
          const datasetUrl = `https://api.apify.com/v2/datasets/${defaultDatasetId}/items?token=${currentToken}`;
          const datasetRes = await fetch(datasetUrl);
          const datasetData = await datasetRes.json();
          
          finalDatasetItems = normalizeData(datasetData);
          success = true;
        } else {
          throw new Error(`Tiến trình cào thất bại với trạng thái: ${runStatus}`);
        }

      } catch (innerError) {
        console.warn(`Lỗi tại Token #${currentTokenIdx + 1}:`, innerError.message);
        currentTokenIdx++; 
        setActiveTokenIndex(currentTokenIdx);
        
        if (currentTokenIdx >= tokens.length) {
           throw new Error("Đã thử hết tất cả Token trong danh sách nhưng đều báo lỗi hoặc hết hạn.");
        }
        
        setStatusMessage(`Token #${currentTokenIdx} lỗi. Đang tự động đổi sang Token #${currentTokenIdx + 1}...`);
        await delay(2000);
      }
    }

    if (success) {
      setResults(finalDatasetItems);
      setStatus('success');
      setStatusMessage(`Hoàn tất! Đã thu thập thành công ${finalDatasetItems.length} bản ghi.`);
    } else {
      setStatus('error');
      setErrorMsg('Không thể hoàn thành việc cào dữ liệu.');
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 font-sans p-4 md:p-8">
      <div className="max-w-[95%] mx-auto space-y-6">
        <header className="flex items-center space-x-3 pb-6 border-b border-slate-200">
          <div className="bg-blue-600 p-3 rounded-xl shadow-lg shadow-blue-600/20">
            <Globe className="w-8 h-8 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-slate-800">Cào Dữ Liệu Facebook (Auto Token)</h1>
            <p className="text-slate-500 text-sm">Tự động lấy token từ file local và gọi trực tiếp 1 lần duy nhất</p>
          </div>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
          <div className="lg:col-span-1 space-y-6">
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
                      <p className="font-semibold flex items-center">
                        <Activity className="w-4 h-4 mr-1.5"/> Đang sử dụng Token #{activeTokenIndex + 1}
                      </p>
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

            <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200">
              <h2 className="text-lg font-semibold mb-4 flex items-center">
                <SlidersHorizontal className="w-5 h-5 mr-2 text-slate-400" /> Cấu hình Tìm Kiếm
              </h2>
              
              <div className="space-y-5">
                <div className="space-y-3 p-4 bg-slate-50 border border-slate-200 rounded-lg">
                  <label className="flex items-center cursor-pointer group">
                    <div className="relative">
                      <input 
                        type="checkbox" 
                        className="sr-only" 
                        checked={strictSearch}
                        onChange={(e) => setStrictSearch(e.target.checked)}
                        disabled={status === 'running'}
                      />
                      <div className={`block w-10 h-6 rounded-full transition-colors ${strictSearch ? 'bg-blue-600' : 'bg-slate-300'}`}></div>
                      <div className={`dot absolute left-1 top-1 bg-white w-4 h-4 rounded-full transition-transform ${strictSearch ? 'transform translate-x-4' : ''}`}></div>
                    </div>
                    <div className="ml-3 text-sm font-medium text-slate-700 group-hover:text-slate-900 transition-colors">
                      Tìm kiếm chính xác
                    </div>
                  </label>

                  <label className="flex items-center cursor-pointer group">
                    <div className="relative">
                      <input 
                        type="checkbox" 
                        className="sr-only" 
                        checked={filterRecent}
                        onChange={(e) => setFilterRecent(e.target.checked)}
                        disabled={status === 'running'}
                      />
                      <div className={`block w-10 h-6 rounded-full transition-colors ${filterRecent ? 'bg-blue-600' : 'bg-slate-300'}`}></div>
                      <div className={`dot absolute left-1 top-1 bg-white w-4 h-4 rounded-full transition-transform ${filterRecent ? 'transform translate-x-4' : ''}`}></div>
                    </div>
                    <div className="ml-3 text-sm font-medium text-slate-700 group-hover:text-slate-900 transition-colors">
                      Lọc bài viết mới nhất
                    </div>
                  </label>

                  <div className="pt-2">
                    <label className="block text-sm font-medium text-slate-700 mb-1 flex items-center">
                      <Clock className="w-4 h-4 mr-1 text-slate-400" /> Khoảng thời gian lấy bài
                    </label>
                    <div className="relative">
                      <select 
                        value={timeInterval}
                        onChange={(e) => setTimeInterval(e.target.value)}
                        disabled={status === 'running'}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none appearance-none cursor-pointer disabled:bg-slate-100 disabled:cursor-not-allowed text-sm pr-10"
                      >
                        <option value="Since 24 Hours">24 giờ qua</option>
                        <option value="Past Week">1 tuần qua</option>
                        <option value="Past Month">1 tháng qua</option>
                        <option value="All Time">Tất cả thời gian</option>
                      </select>
                      <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3 text-slate-500">
                        <svg className="w-4 h-4 fill-current" viewBox="0 0 20 20">
                          <path d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" />
                        </svg>
                      </div>
                    </div>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1 flex items-center">
                    <TableIcon className="w-4 h-4 mr-1 text-slate-400" /> Mục tiêu cào tổng cộng
                  </label>
                  <input 
                    type="number" 
                    value={targetItems} 
                    disabled
                    className="w-full px-4 py-2 bg-slate-200 text-slate-600 border border-slate-300 rounded-lg outline-none cursor-not-allowed font-semibold" 
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-2 flex items-center">
                    <Search className="w-4 h-4 mr-1 text-slate-400" /> Chọn từ khóa để cào tự động
                  </label>
                  <div className="flex flex-col gap-2">
                    {['mua bán nhà', 'mua bán đất', 'mua bán động sản'].map((kw) => (
                      <button 
                        key={kw}
                        onClick={() => startCrawling(kw)} 
                        disabled={status === 'running' || tokens.length === 0}
                        className={`w-full py-2.5 px-4 rounded-lg font-medium flex items-center justify-between transition-all shadow-sm border
                          ${status === 'running' || tokens.length === 0 
                            ? 'bg-slate-100 text-slate-400 border-slate-200 cursor-not-allowed' 
                            : 'bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-100 hover:border-blue-300 active:scale-[0.98]'
                          }
                          ${keyword === kw && status === 'running' ? 'ring-2 ring-blue-500 bg-blue-100 text-blue-800' : ''}
                        `}
                      >
                        <span className="flex items-center">
                          {keyword === kw && status === 'running' ? (
                            <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                          ) : (
                            <Search className="w-4 h-4 mr-2 opacity-70" />
                          )}
                          {kw}
                        </span>
                        <Play className="w-4 h-4 opacity-50" />
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </div>

            {(status !== 'idle' || errorMsg) && (
              <div ref={statusRef} className={`p-4 rounded-xl border ${status === 'error' ? 'bg-red-50 border-red-200' : status === 'success' ? 'bg-green-50 border-green-200' : 'bg-blue-50 border-blue-200'}`}>
                <div className="flex items-start">
                  {status === 'error' && <AlertCircle className="w-5 h-5 text-red-500 mr-2 mt-0.5" />}
                  {status === 'success' && <CheckCircle2 className="w-5 h-5 text-green-500 mr-2 mt-0.5" />}
                  {status === 'running' && <RefreshCw className="w-5 h-5 text-blue-500 mr-2 mt-0.5 animate-spin" />}
                  <div>
                    <h3 className={`font-semibold text-sm ${status === 'error' ? 'text-red-800' : status === 'success' ? 'text-green-800' : 'text-blue-800'}`}>{status === 'error' ? 'Lỗi' : status === 'success' ? 'Hoàn tất' : 'Đang xử lý ngầm'}</h3>
                    <p className={`text-sm mt-1 ${status === 'error' ? 'text-red-600' : status === 'success' ? 'text-green-600' : 'text-blue-600'}`}>{errorMsg || statusMessage}</p>
                  </div>
                </div>
              </div>
            )}
          </div>

          <div className="lg:col-span-3">
            <div className="bg-white rounded-2xl shadow-sm border border-slate-200 overflow-hidden h-full flex flex-col">
              <div className="p-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
                <div className="flex items-center space-x-3">
                  <h2 className="text-lg font-semibold flex items-center">
                    <TableIcon className="w-5 h-5 mr-2 text-slate-400" /> Dữ Liệu Thu Thập Được
                  </h2>
                  <span className="text-xs font-medium bg-slate-200 text-slate-600 px-2.5 py-1 rounded-full">
                    {results.length} bản ghi
                  </span>
                </div>

                {results.length > 0 && (
                  <button
                    onClick={exportToExcel}
                    className="inline-flex items-center px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-semibold rounded-lg shadow-sm transition-all active:scale-95"
                  >
                    <FileSpreadsheet className="w-4 h-4 mr-2" />
                    Xuất ra file Excel
                  </button>
                )}
              </div>
              
              <div className="p-0 overflow-auto flex-grow" style={{ minHeight: '450px', maxHeight: '700px' }}>
                {results.length === 0 ? (
                  <div className="h-full flex flex-col items-center justify-center text-slate-400 p-8 text-center" style={{ minHeight: '400px' }}>
                    <TableIcon className="w-12 h-12 mb-3 opacity-20" />
                    <p>Chưa có dữ liệu nào được trích xuất.</p>
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse min-w-[1500px]">
                      <thead>
                        <tr className="bg-slate-100 text-slate-600 text-xs uppercase tracking-wider border-b border-slate-200 sticky top-0">
                          {COLUMNS.map((col) => (
                            <th key={col.key} className="p-3 font-semibold border-r border-slate-200 last:border-r-0">
                              {col.label}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 text-sm">
                        {results.map((row, idx) => (
                          <tr key={row.postId || idx} className="hover:bg-slate-50/80 transition-colors">
                            {COLUMNS.map((col) => {
                              const val = row[col.key];

                              if (col.key === 'postUrl') {
                                return (
                                  <td key={col.key} className="p-3 border-r border-slate-100 max-w-[220px]">
                                    {val && val !== '#' ? (
                                      <a 
                                        href={val} 
                                        target="_blank" 
                                        rel="noopener noreferrer" 
                                        className="inline-flex items-center px-2.5 py-1 bg-blue-50 text-blue-600 hover:bg-blue-100 rounded-md text-xs font-medium transition-colors truncate max-w-full"
                                        title={val}
                                      >
                                        <span className="truncate">Mở bài viết</span>
                                        <ExternalLink className="w-3 h-3 ml-1 flex-shrink-0" />
                                      </a>
                                    ) : <span className="text-slate-400">Không có</span>}
                                  </td>
                                );
                              }

                              if (col.key === 'createTime') {
                                return (
                                  <td key={col.key} className="p-3 border-r border-slate-100 whitespace-nowrap text-slate-600 text-xs">
                                    {val || 'Không có'}
                                  </td>
                                );
                              }

                              if (col.key === 'authorProfileUrl') {
                                return (
                                  <td key={col.key} className="p-3 border-r border-slate-100 max-w-[150px] truncate">
                                    {val && val !== '#' ? (
                                      <a href={val} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline flex items-center text-xs">
                                        Trang cá nhân <ExternalLink className="w-3 h-3 ml-1 flex-shrink-0" />
                                      </a>
                                    ) : 'Không có'}
                                  </td>
                                );
                              }
                              
                              if (col.key === 'imageList' || col.key === 'videoList') {
                                return (
                                  <td key={col.key} className="p-3 border-r border-slate-100 text-slate-500 text-xs">
                                    {Array.isArray(val) && val.length > 0 ? `${val.length} tệp` : '0'}
                                  </td>
                                );
                              }

                              return (
                                <td key={col.key} className="p-3 border-r border-slate-100 text-slate-700 max-w-[250px] truncate" title={typeof val === 'string' ? val : ''}>
                                  {val !== undefined && val !== null && val !== '' ? String(val) : <span className="text-slate-300 italic">Trống</span>}
                                </td>
                              );
                            })}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}