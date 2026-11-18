(() => {
    const el = id => document.getElementById(id);
    let runId = sessionStorage.getItem('socialBotRun');
    let rows = [];
    let completedRunId = null;
    const message = text => { el('crawlConnectionStatus').textContent = text; };
    const busy = value => {
        el('crawlStart').disabled = value;
        el('crawlStop').disabled = !value;
        el('crawlLimit').disabled = value;
        el('crawlInterval').disabled = value;
    };
    async function request(action, data, method = 'POST') {
        const response = await fetch(`/Admin/SocialBot/${action}`, method === 'GET' ? {} : {
            method, headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value },
            body: new URLSearchParams(data)
        });
        const result = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(result.message || 'Không thực hiện được yêu cầu.');
        return result;
    }
    function render(items) {
        rows = items.filter(x => x.type === 'post');
        el('crawlResultCount').textContent = `${rows.length} bài`;
        el('crawlExport').disabled = !rows.length;
        if (!rows.length) {
            el('crawlResultsBody').innerHTML = '<tr><td colspan="5" class="text-center text-muted py-5">Chưa có kết quả thu thập.</td></tr>';
            return;
        }
        el('crawlResultsBody').replaceChildren();
        for (const item of rows) {
            const tr = document.createElement('tr');
            const date = new Date(item.postedAt);
            for (const value of [Number.isNaN(date.getTime()) ? '' : date.toLocaleString('vi-VN'), item.text, item.author?.name,
                `Thích: ${item.reactions ?? 0} · Bình luận: ${item.commentsCount ?? 0} · Chia sẻ: ${item.shares ?? 0}`]) {
                const td = document.createElement('td');
                td.textContent = value || '';
                td.style.whiteSpace = 'pre-wrap';
                tr.append(td);
            }
            const td = document.createElement('td');
            try {
                const url = new URL(item.permalink || item.url);
                if (url.protocol === 'https:' && (url.hostname === 'facebook.com' || url.hostname.endsWith('.facebook.com'))) {
                    const a = document.createElement('a');
                    a.href = url.href; a.target = '_blank'; a.rel = 'noopener noreferrer'; a.textContent = 'Mở bài'; td.append(a);
                }
            } catch { /* Bỏ qua URL không hợp lệ. */ }
            tr.append(td); el('crawlResultsBody').append(tr);
        }
    }
    async function poll() {
        try {
            const result = await request(`CrawlStatus?runId=${encodeURIComponent(runId)}`, null, 'GET');
            message(`Trạng thái Apify: ${result.status}`);
            if (['SUCCEEDED', 'ABORTED', 'FAILED', 'TIMED-OUT'].includes(result.status)) {
                render(result.items || []);
                completedRunId = ['SUCCEEDED', 'ABORTED'].includes(result.status) ? runId : null;
                el('crawlSave').disabled = !completedRunId || !rows.length;
                runId = null;
                // Giữ ID phiên hoàn tất để tải lại trang vẫn có thể lưu kết quả.
                if (!completedRunId) sessionStorage.removeItem('socialBotRun');
                busy(false); return;
            }
            setTimeout(poll, 4000);
        } catch (error) {
            message(`${error.message} Kiểm tra Apify Console; tải lại trang để thử theo dõi tiếp.`);
            // Giữ ID để có thể dừng hoặc tiếp tục theo dõi, không tự tạo run mới.
        }
    }
    el('crawlStart').addEventListener('click', async () => {
        if (!el('crawlLimit').reportValidity()) return;
        completedRunId = null; el('crawlSave').disabled = true; render([]);
        busy(true); message('Đang khởi tạo phiên thu thập…');
        try {
            const result = await request('StartCrawl', { limit: el('crawlLimit').value, days: el('crawlInterval').value });
            runId = result.runId; sessionStorage.setItem('socialBotRun', runId); poll();
        } catch (error) { message(error.message); busy(false); }
    });
    el('crawlStop').addEventListener('click', async () => {
        if (!runId) return;
        el('crawlStop').disabled = true;
        try { await request('StopCrawl', { runId }); message('Đã gửi yêu cầu dừng tới Apify.'); }
        catch (error) { message(error.message); el('crawlStop').disabled = false; }
    });
    el('crawlSave').addEventListener('click', async () => {
        if (!completedRunId) return;
        el('crawlSave').disabled = true;
        el('crawlStart').disabled = true;
        message('Đang lưu kết quả vào database…');
        try {
            const result = await request('SaveCrawl', { runId: completedRunId });
            message(`Đã lưu ${result.saved} bài mới; bỏ qua ${result.duplicate} bài trùng và ${result.skipped} bài thiếu nội dung, định danh hoặc nguồn hợp lệ.`);
        } catch (error) { message(error.message); }
        finally { el('crawlSave').disabled = false; el('crawlStart').disabled = false; }
    });
    el('crawlExport').addEventListener('click', () => {
        const cell = value => '"' + String(value ?? '').replace(/^[=+@\-]/, "'$&").replaceAll('"', '""') + '"';
        const csv = [['Thời gian', 'Nội dung', 'Tác giả', 'URL'], ...rows.map(x => [x.postedAt, x.text, x.author?.name, x.permalink || x.url])];
        const url = URL.createObjectURL(new Blob(['\uFEFF' + csv.map(r => r.map(cell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' }));
        const a = document.createElement('a'); a.href = url; a.download = 'facebook-bat-dong-san.csv'; a.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
    });
    if (runId) { busy(true); poll(); }
})();
