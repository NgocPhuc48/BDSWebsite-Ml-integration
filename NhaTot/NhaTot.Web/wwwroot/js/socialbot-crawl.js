(() => {
    const el = id => document.getElementById(id);
    let runId = sessionStorage.getItem('socialBotRun');
    let currentKeyword = sessionStorage.getItem('socialBotKeyword') || '';
    let crawlMode = sessionStorage.getItem('socialBotMode') || 'group';
    let lastRunId = runId;
    let rows = [];

    const message = text => { if (el('crawlConnectionStatus')) el('crawlConnectionStatus').textContent = text; };
    const setBusy = value => {
        document.querySelectorAll('.keyword-crawl, .group-quick-crawl').forEach(x => x.disabled = value);
        if (el('groupCrawlStart')) el('groupCrawlStart').disabled = value;
        if (el('crawlStop')) el('crawlStop').disabled = !value;

        for (const id of [
            'crawlLimit', 'crawlRegion', 'crawlInterval', 'strictSearch', 'filterRecent',
            'groupRegion', 'groupsPerSearch', 'minMembers', 'groupPostAge', 'postsPerGroup',
            'groupSearchTerms', 'groupNewestFirst', 'groupIncludeComments'
        ]) {
            if (el(id)) el(id).disabled = value;
        }
    };

    async function request(action, data, method = 'POST') {
        const response = await fetch(`/Admin/SocialBot/${action}`, method === 'GET' ? {} : {
            method,
            headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value },
            body: new URLSearchParams(data)
        });
        const result = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(result.message || 'Không thực hiện được yêu cầu.');
        return result;
    }

    const normalizeText = value => String(value ?? '')
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/g, 'd')
        .normalize('NFC');

    const BUY_SELL_PATTERN = /\b(mua ban|can ban|ban gap|ban nhanh|ban re|ban lo|ban nha|ban dat|ban can ho|ban chung cu|ban biet thu|ban mat bang|ban kho|ban xuong|mua nha|mua dat|can mua|tim mua|rao ban|mo ban|chao ban|gui ban|ky gui|sang nhuong|chuyen nhuong|cat lo|gia ban|chinh chu ban|ban|mua)\b/;
    const RENT_ONLY_PATTERN = /\b(cho thue|can thue|tim thue|phong tro|nha tro|o ghep|thue nha|thue dat|thue phong|thue can ho|thue chung cu|thue mat bang|thue kho|thue xuong)\b/;
    const HOUSING_RENT_ACTION_PATTERN = /\b(cho thue|can thue|tim thue|thue nha|thue phong|thue tro|thue can ho|thue chung cu|thue mat bang|thue ki\s*ot|thue kiot|thue nguyen can|thue dat|thue kho|thue xuong|o ghep|tim nguoi o ghep|nhuong phong|pass phong|sang nhuong mat bang|sang mat bang)\b/;
    const HOUSING_RENT_NOUN_PATTERN = /\b(nha|nha nguyen can|nha rieng|nha cap 4|phong|phong tro|nha tro|khep kin|can ho|chung cu|biet thu|mat bang|ki\s*ot|kiot|cua hang|van phong|dat|kho|xuong)\b/;
    const SALE_IN_RENT_MODE_PATTERN = /\b(can ban|ban gap|ban nhanh|ban re|ban lo|ban nha|ban dat|ban can ho|ban chung cu|ban biet thu|ban mat bang|ban kho|ban xuong|mua ban|rao ban|mo ban|chao ban|gui ban|chinh chu ban|gia ban|sang ten so|so do chinh chu|so hong rieng|dat dau gia|phan lo ban nen|cat lo)\b/;
    const NON_HOUSING_RENT_PATTERN = /\b(cho thue xe|thue xe|xe tu lai|xe du lich|xe tai|xe cau|xe ba gac|chuyen nha|chuyen tro|chuyen van phong|boc vac|may xuc|may dao|gian giao|cop pha|loa dai|am thanh|anh sang|ban ghe|rap cuoi|nha bat|vay cuoi|ao cuoi|ao dai|trang phuc|may chieu|may anh|flycam|giup viec|tap vu|bao ve|gia su|ke toan|khai thue|nop thue|thue thu nhap|thue suat|thue tai khoan|chay ads)\b/;
    const EXPLICIT_BUY_SELL_PATTERN = /\b(mua ban|can ban|ban gap|ban nhanh|ban nha|ban dat|ban can ho|ban chung cu|mua nha|mua dat|can mua|tim mua|sang nhuong|chuyen nhuong|rao ban|mo ban|chao ban)\b/;
    const IRRELEVANT_PATTERN = /\b(tuyen dung|tuyen nhan vien|tuyen ctv|tim viec|viec lam|vay von|cho vay|cam do|my pham|quan ao|giay dep|sim so dep|ca cuoc|lo de|cho thue xe|thue xe tu lai|thue vay|thue ao cuoi|thue loa dai)\b/;
    const NEWS_CRIME_PATTERN = /\b(khoi to|doi tuong|lua dao|chiem doat|bat giu|bat tam giam|tam giam|cong an|canh sat|co quan dieu tra|vien kiem sat|toa an|xet xu|bi can|bi cao|truy to|pham toi|thu doan|gian doi|huy dong von|vu an|vi pham phap luat|trom cap|cuop giat|tai nan giao thong|tin nong|nong:)\b/;
    const DISCUSSION_QUESTION_PATTERN = /\b(cho minh hoi|cho em hoi|cho toi hoi|moi nguoi cho hoi|anh chi cho hoi|cac bac cho hoi|ai da mua|anh chi nao da mua|co nen mua|xin kinh nghiem|xin review|xin y kien|nho moi nguoi tu van|nho anh chi tu van|cac bac tu van|tu van giup minh|tu van giup em|hoi kinh nghiem|hoi that|dang tim hieu|muon nghe so lieu|theo moi nguoi)\b/;
    const REAL_ESTATE_PATTERN = /\b(nha|dat|bat dong san|bds|can ho|chung cu|biet thu|dat nen|lo dat|nha pho|nha rieng|shophouse|so do|so hong|tho cu|mat tien|dien tich|m2|phong tro|nha tro|mat bang|ki ot|kiot|nguyen can|khep kin)\b/;
    const PRICE_PATTERN = /(?:\b\d+(?:[.,]\d+)?\s*(?:ty|ti|trieu|tr|cu|k|nghin|ngan|vnd)(?:\s*ruoi)?(?:\s*\/\s*(?:\d+\s*)?(?:thang|th|nam|m2?))?\b|\b\d+\s*(?:ty|ti|tr|trieu)\s*(?:\d+|x+|ruoi)\b|\b\d+(?:[.,]\d+)?\s*(?:tr|trieu|ty|ti|d)\s*\/\s*m2?\b|\b\d{1,3}(?:[.,]\d{3}){1,}\s*(?:d|dong|vnd)?(?:\s*\/\s*(?:thang|th))?\b)/;
    const ADDRESS_UNIT_PATTERN = /\b(?:dia chi|vi tri|khu vuc|toa lac|gan|cach|duong|pho|ngo|ngach|hem|thon|xom|ap|khu pho|khoi|phuong|xa|thi tran|quan|huyen|thi xa|thanh pho|tp\.?|tinh|kdt|khu do thi|kdc|khu dan cu|chung cu|toa|du an|dai hoc|dh)\s+[a-z0-9]/;
    const ADDRESS_ABBR_PATTERN = /\b(?:p|q|h|tx|tt|x|kp)\.\s*[a-z0-9]/;
    const ADDRESS_PLACE_PATTERN = /\b(?:nghe an|tp vinh|thanh pho vinh|vinh|dai hoc vinh|dh vinh|ecopark vinh|cua lo|nghi loc|hung nguyen|dien chau|quynh luu|hoang mai|thai hoa|nam dan|do luong|thanh chuong|yen thanh|nghia dan|quy hop|quy chau|que phong|tan ky|anh son|con cuong|tuong duong|ky son|ben thuy|trung do|truong thi|hung dung|hung binh|hung phuc|hung loc|hung dong|hung chinh|hung hoa|quang trung|le loi|le mao|ha huy tap|quan bau|dong vinh|doi cung|cua nam|vinh tan|nghi phu|nghi kim|nghi an|nghi duc|nghi lien|ha tinh|hong linh|ky anh|nghi xuan|duc tho|huong son|huong khe|can loc|cam xuyen|thach ha|loc ha|vu quang|bac ha|nam ha|tran phu|tan giang|thach quy|thach linh|dai nai|van yen)\b/;
    const OTHER_PROVINCE_PATTERN = /\b(?:ha noi|tp\s*hcm|tphcm|sai gon|quan\s*\d+|go vap|binh thanh|tan binh|thu duc|da nang|hai phong|can tho|binh duong|dong nai|long an|vung tau|nha trang|da lat|bac ninh|hung yen|quang ninh|thanh hoa)\b/;

    function getPostText(item) {
        return item?.text || item?.postText || item?.content || item?.description || item?.message || '';
    }

    function getPostUrl(item) {
        if (!item || typeof item !== 'object') return '';
        const candidates = [
            item.postUrl, item.post_url, item.postLink, item.post_link,
            item.permalink, item.permalinkUrl, item.permalink_url,
            item.webUrl, item.url, item.link,
            item.facebookUrl, item.fbUrl, item.shareUrl, item.topLevelUrl, item.postPermalink
        ];
        for (const raw of candidates) {
            if (typeof raw === 'string' && raw.trim()) return raw.trim();
        }
        const postId = item.postId || item.post_id || item.id;
        const authorId = item.author_user_id || item.authorUserId || item.authorId || item.userId;
        if (postId) {
            return authorId
                ? `https://www.facebook.com/${authorId}/posts/${postId}`
                : `https://www.facebook.com/${postId}`;
        }
        return '';
    }

    function getExtraMeta(item) {
        return normalizeText([
            item?.location,
            item?.place?.name,
            item?.address,
            item?.groupName,
            item?.inputUrl
        ].filter(Boolean).join(' ')).trim();
    }

    function hasPrice(text) {
        return PRICE_PATTERN.test(text);
    }

    function hasAddress(text, item) {
        const extra = getExtraMeta(item);
        return ADDRESS_UNIT_PATTERN.test(text)
            || ADDRESS_ABBR_PATTERN.test(text)
            || ADDRESS_PLACE_PATTERN.test(text)
            || (extra.length >= 3 && (ADDRESS_UNIT_PATTERN.test(extra) || ADDRESS_PLACE_PATTERN.test(extra)));
    }

    function isTargetRegion(text, item) {
        if (OTHER_PROVINCE_PATTERN.test(text)) return false;
        const extra = getExtraMeta(item);
        if (OTHER_PROVINCE_PATTERN.test(extra)) return false;
        if (ADDRESS_PLACE_PATTERN.test(text) || ADDRESS_PLACE_PATTERN.test(extra)) return true;
        // Với chế độ cho thuê (đã tìm kiếm giới hạn theo vùng Nghệ An / Hà Tĩnh), chấp nhận bài có đường/phường rõ ràng nếu không dính tỉnh khác
        if (isRentMode() && hasAddress(text, item)) return true;
        return false;
    }

    function isNewsOrCrimePost(text) {
        const deobfuscated = text.replace(/(?<=[a-z])[._\-]+(?=[a-z])/g, '');
        return NEWS_CRIME_PATTERN.test(text) || NEWS_CRIME_PATTERN.test(deobfuscated);
    }

    function getPostTimestamp(item) {
        const raw = item?.postedAt || item?.createTime || item?.createdAt || item?.time;
        if (typeof raw === 'number') return raw > 1e12 ? raw : raw * 1000;
        const ms = Date.parse(raw);
        return Number.isNaN(ms) ? 0 : ms;
    }

    function matchesStrictKeyword(text) {
        if (crawlMode !== 'keyword' || !el('strictSearch')?.checked) return true;
        // Chuẩn hóa bỏ dấu TRƯỚC rồi mới xóa tên tỉnh để không bị lỗi \b với tiếng Việt có dấu
        const baseKw = normalizeText(currentKeyword || '')
            .replace(/[()]/g, ' ')
            .replace(/\b(nghe an|ha tinh)\b/g, '')
            .replace(/\s+/g, ' ')
            .trim();
        if (!baseKw) return true;
        // Từ khóa tổng hợp ("mua bat dong san", "thue bat dong san") không bắt buộc bài viết phải có đúng chữ "bat dong san"
        if (baseKw.includes('bat dong san')) return REAL_ESTATE_PATTERN.test(text);
        if (baseKw === 'thue dat') return /\b(dat|mat bang|ki ot|kiot|kho|xuong)\b/.test(text);
        if (baseKw === 'thue nha') return /\b(nha|phong tro|can ho|chung cu|ki ot|mat bang)\b/.test(text);
        const tokens = baseKw.split(/\s+/).filter(Boolean);
        return tokens.every(t => text.includes(t));
    }

    function matchesRecentInterval(item) {
        if (crawlMode === 'group') {
            const age = el('groupPostAge')?.value || '';
            const maxAgeMs = age === '1 day' ? 24 * 3600 * 1000
                : age === '7 days' ? 7 * 24 * 3600 * 1000
                : age === '2 weeks' ? 14 * 24 * 3600 * 1000
                : age === '30 days' ? 30 * 24 * 3600 * 1000
                : 0;
            if (!maxAgeMs) return true;
            const ts = getPostTimestamp(item);
            return !ts || (Date.now() - ts) <= maxAgeMs;
        }
        if (!el('filterRecent')?.checked) return true;
        const interval = el('crawlInterval')?.value || '';
        const maxAgeMs = interval === '24h' || interval === '1d' ? 24 * 3600 * 1000
            : interval === '7d' ? 7 * 24 * 3600 * 1000
            : interval === '30d' ? 30 * 24 * 3600 * 1000
            : 0;
        if (!maxAgeMs) return true;
        const ts = getPostTimestamp(item);
        return !ts || (Date.now() - ts) <= maxAgeMs;
    }

    function isRentMode() {
        return /\bthue\b/.test(normalizeText(currentKeyword));
    }

    function isBuySellPost(item) {
        const text = normalizeText(getPostText(item)).trim();
        if (text.length < 10) return false;
        if (isNewsOrCrimePost(text)) return false;
        if (DISCUSSION_QUESTION_PATTERN.test(text)) return false;
        if (!matchesStrictKeyword(text)) return false;
        if (!matchesRecentInterval(item)) return false;
        if (isRentMode()) {
            // Loại bỏ mọi bài rao bán (kể cả bài bán nhà gắn mác "đang cho thuê dòng tiền")
            if (SALE_IN_RENT_MODE_PATTERN.test(text)) return false;
            if (/\b\d+(?:[.,]\d+)?\s*(?:ty|ti)\b/.test(text) && !/\/\s*(?:thang|th|nam)\b/.test(text)) return false;
            // Loại bỏ dịch vụ vận chuyển, thuê xe, thuê đồ, thuế... không phải thuê nhà/BĐS
            if (NON_HOUSING_RENT_PATTERN.test(text)) return false;
            // Bắt buộc phải có cả hành động thuê/cho thuê lẫn danh từ nhà/phòng/căn hộ/mặt bằng
            if (!HOUSING_RENT_ACTION_PATTERN.test(text) || !HOUSING_RENT_NOUN_PATTERN.test(text)) return false;
        } else {
            if (!BUY_SELL_PATTERN.test(text)) return false;
            if (RENT_ONLY_PATTERN.test(text) && !EXPLICIT_BUY_SELL_PATTERN.test(text)) return false;
        }
        if (IRRELEVANT_PATTERN.test(text)) return false;
        if (!hasPrice(text)) return false;
        if (!hasAddress(text, item)) return false;
        if (!isTargetRegion(text, item)) return false;
        return true;
    }

    function getCommentTotal(item) {
        if (typeof item?.commentCount === 'number') return item.commentCount;
        if (typeof item?.commentsCount === 'number') return item.commentsCount;
        if (typeof item?.comment_count === 'number') return item.comment_count;
        if (Array.isArray(item?.comments)) return item.comments.length;
        return Number(item?.comments) || 0;
    }

    function buildCommentsDetails(item) {
        if (!Array.isArray(item?.comments) || item.comments.length === 0) return null;
        const details = document.createElement('details');
        details.className = 'mt-2 small text-muted';
        const summary = document.createElement('summary');
        summary.className = 'fw-semibold text-primary';
        summary.style.cursor = 'pointer';
        summary.textContent = `Xem ${item.comments.length} bình luận thu thập được`;
        details.append(summary);

        const list = document.createElement('ul');
        list.className = 'list-unstyled ps-2 mt-1 mb-0 border-start border-2';
        for (const c of item.comments.slice(0, 10)) {
            const li = document.createElement('li');
            li.className = 'mb-1';
            const cAuthor = c.authorName || c.author?.name || 'Người dùng';
            const cText = c.text || c.message || '';
            const strong = document.createElement('strong');
            strong.textContent = `${cAuthor}: `;
            li.append(strong, document.createTextNode(cText));

            if (Array.isArray(c.replies) && c.replies.length > 0) {
                const sub = document.createElement('ul');
                sub.className = 'list-unstyled ps-3 mt-1 text-secondary';
                for (const r of c.replies.slice(0, 5)) {
                    const rLi = document.createElement('li');
                    const rAuthor = r.authorName || r.author?.name || 'Phản hồi';
                    const rStrong = document.createElement('strong');
                    rStrong.textContent = `↳ ${rAuthor}: `;
                    rLi.append(rStrong, document.createTextNode(r.text || r.message || ''));
                    sub.append(rLi);
                }
                li.append(sub);
            }
            list.append(li);
        }
        details.append(list);
        return details;
    }

    function render(items) {
        const rawPosts = (items || []).filter(x => (!x.type || x.type === 'post') && getPostText(x).trim().length > 0);
        rows = rawPosts.filter(isBuySellPost);
        const shouldSortNewest = crawlMode === 'group' ? el('groupNewestFirst')?.checked : el('filterRecent')?.checked;
        if (shouldSortNewest) {
            rows.sort((a, b) => getPostTimestamp(b) - getPostTimestamp(a));
        }
        const filteredOut = rawPosts.length - rows.length;
        el('crawlResultCount').textContent = filteredOut > 0
            ? `${rows.length} bài (đã lọc ${filteredOut})`
            : `${rows.length} bài`;
        el('crawlExport').disabled = !rows.length;
        el('crawlSave').disabled = !rows.length;
        if (!rows.length) {
            const emptyMsg = rawPosts.length > 0
                ? `Đã lọc toàn bộ ${rawPosts.length} bài không hợp lệ (không phải mua bán hoặc thiếu giá/địa chỉ).`
                : 'Chưa có dữ liệu nào được trích xuất.';
            el('crawlResultsBody').innerHTML = `<tr><td colspan="5" class="text-center text-muted py-5">${emptyMsg}</td></tr>`;
            return;
        }
        el('crawlResultsBody').replaceChildren();
        for (const item of rows) {
            const tr = document.createElement('tr');
            const ts = getPostTimestamp(item);
            const dateStr = ts ? new Date(ts).toLocaleString('vi-VN') : '';
            const authorName = item.author?.name || item.user?.name || item.authorName || item.pageName || '';
            const groupName = item.groupName || '';
            const likes = typeof item.reactions === 'number' ? item.reactions : (item.likes ?? item.likesCount ?? item.like_count ?? 0);
            const comments = getCommentTotal(item);
            const shares = item.shareCount ?? item.shares ?? item.sharesCount ?? item.share_count ?? 0;

            // Cột 1: Thời gian
            const tdDate = document.createElement('td');
            tdDate.textContent = dateStr;
            tdDate.className = 'text-nowrap small';
            tr.append(tdDate);

            // Cột 2: Nội dung + bình luận (nếu có)
            const tdContent = document.createElement('td');
            const contentDiv = document.createElement('div');
            contentDiv.textContent = getPostText(item);
            contentDiv.style.whiteSpace = 'pre-wrap';
            tdContent.append(contentDiv);
            const commentsNode = buildCommentsDetails(item);
            if (commentsNode) tdContent.append(commentsNode);
            tr.append(tdContent);

            // Cột 3: Tác giả / Group
            const tdAuthor = document.createElement('td');
            const authorDiv = document.createElement('div');
            authorDiv.className = 'fw-semibold';
            authorDiv.textContent = authorName || '—';
            tdAuthor.append(authorDiv);
            if (groupName) {
                const groupDiv = document.createElement('div');
                groupDiv.className = 'small text-muted';
                groupDiv.textContent = `Group: ${groupName}`;
                tdAuthor.append(groupDiv);
            }
            tr.append(tdAuthor);

            // Cột 4: Tương tác
            const tdStats = document.createElement('td');
            tdStats.className = 'small text-nowrap';
            tdStats.textContent = `Thích: ${likes} · BL: ${comments} · CS: ${shares}`;
            tr.append(tdStats);

            // Cột 5: Link bài viết
            const tdLink = document.createElement('td');
            const rawUrl = getPostUrl(item);
            try {
                const url = new URL(rawUrl);
                const host = url.hostname.toLowerCase();
                if ((url.protocol === 'https:' || url.protocol === 'http:')
                    && (host === 'facebook.com' || host.endsWith('.facebook.com') || host === 'fb.com' || host.endsWith('.fb.com') || host === 'fb.watch')) {
                    const a = document.createElement('a');
                    a.href = url.href;
                    a.target = '_blank';
                    a.rel = 'noopener noreferrer';
                    a.textContent = url.href;
                    a.style.wordBreak = 'break-all';
                    tdLink.append(a);
                }
            } catch { /* Bỏ qua URL không hợp lệ. */ }
            tr.append(tdLink);

            el('crawlResultsBody').append(tr);
        }
    }

    async function saveRowsToDatabase(isAuto = false) {
        if (!rows.length) return;
        const saveBtn = el('crawlSave');
        if (saveBtn) saveBtn.disabled = true;
        message(isAuto ? `Đang lưu nóng ${rows.length} bài vào database…` : `Đang lưu ${rows.length} bài vào database…`);
        try {
            const response = await fetch('/Admin/SocialBot/SaveKeywordPosts', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify({
                    keyword: currentKeyword,
                    runId: lastRunId,
                    items: rows
                })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result.message || 'Không lưu được vào database.');
            message(`Đã lưu nóng ${result.saved} bài vào database (trùng: ${result.duplicate}, bỏ qua: ${result.skipped}). Xem bảng tại "Dữ liệu đã thu thập".`);
        } catch (error) {
            message(`Lỗi lưu database: ${error.message}`);
            if (saveBtn) saveBtn.disabled = false;
        }
    }

    async function poll() {
        try {
            const result = await request(`CrawlStatus?runId=${encodeURIComponent(runId)}`, null, 'GET');
            message(`Trạng thái Apify: ${result.status}`);
            if (['SUCCEEDED', 'ABORTED', 'FAILED', 'TIMED-OUT'].includes(result.status)) {
                lastRunId = runId;
                render(result.items || []);
                runId = null;
                sessionStorage.removeItem('socialBotRun');
                setBusy(false);
                if (result.status === 'SUCCEEDED' || result.status === 'ABORTED') {
                    if (rows.length > 0) {
                        await saveRowsToDatabase(true);
                    } else if (result.status === 'SUCCEEDED') {
                        message('Hoàn tất! Không có bài hợp lệ (có đủ giá & địa chỉ tại Nghệ An / Hà Tĩnh) để lưu.');
                    }
                }
                return;
            }
            setTimeout(poll, 4000);
        } catch (error) {
            message(`${error.message} Kiểm tra Apify Console; tải lại trang để theo dõi tiếp.`);
        }
    }

    async function startGroupCrawl(overrideTerms = null) {
        const rawTerms = (overrideTerms ?? el('groupSearchTerms')?.value ?? '').trim();
        if (!rawTerms) {
            message('Vui lòng nhập ít nhất 1 từ khóa chủ đề để tìm Group.');
            return;
        }
        crawlMode = 'group';
        sessionStorage.setItem('socialBotMode', crawlMode);
        setBusy(true);
        render([]);
        const region = el('groupRegion')?.value || 'Nghệ An';
        currentKeyword = `${rawTerms.replace(/\s+/g, ' ')} (${region})`;
        sessionStorage.setItem('socialBotKeyword', currentKeyword);
        message(`Đang tìm Group công khai & lấy bài viết cho "${currentKeyword}"…`);
        try {
            const includeComments = !!el('groupIncludeComments')?.checked;
            const result = await request('StartGroupSearchCrawl', {
                searchTerms: rawTerms,
                region,
                groupsPerSearch: el('groupsPerSearch')?.value || 5,
                minMembers: el('minMembers')?.value || 1000,
                maxPosts: el('postsPerGroup')?.value || 10,
                onlyPostsNewerThan: el('groupPostAge')?.value || '',
                newestFirst: !!el('groupNewestFirst')?.checked,
                includeComments,
                includeReplies: includeComments
            });
            runId = result.runId;
            lastRunId = runId;
            sessionStorage.setItem('socialBotRun', runId);
            poll();
        } catch (error) {
            message(error.message);
            setBusy(false);
        }
    }

    if (el('groupCrawlStart')) {
        el('groupCrawlStart').addEventListener('click', () => startGroupCrawl());
    }

    document.querySelectorAll('.group-quick-crawl').forEach(button => {
        button.addEventListener('click', () => startGroupCrawl(button.dataset.keyword));
    });

    document.querySelectorAll('.keyword-crawl').forEach(button => {
        button.addEventListener('click', async () => {
            crawlMode = 'keyword';
            sessionStorage.setItem('socialBotMode', crawlMode);
            setBusy(true);
            render([]);
            const region = el('crawlRegion')?.value || 'Nghệ An';
            currentKeyword = `${button.dataset.keyword || ''} ${region}`.trim();
            sessionStorage.setItem('socialBotKeyword', currentKeyword);
            message(`Đang cào từ khóa "${currentKeyword}"…`);
            try {
                const result = await request('StartKeywordCrawl', {
                    keyword: button.dataset.keyword,
                    region,
                    strictSearch: el('strictSearch').checked,
                    filterRecent: el('filterRecent').checked,
                    timeInterval: el('crawlInterval').value,
                    limit: el('crawlLimit').value
                });
                runId = result.runId;
                lastRunId = runId;
                sessionStorage.setItem('socialBotRun', runId);
                poll();
            } catch (error) {
                message(error.message);
                setBusy(false);
            }
        });
    });

    el('crawlSave').addEventListener('click', () => saveRowsToDatabase(false));

    el('crawlStop').addEventListener('click', async () => {
        if (!runId) return;
        el('crawlStop').disabled = true;
        try {
            await request('StopCrawl', { runId });
            message('Đã gửi yêu cầu dừng tới Apify.');
        } catch (error) {
            message(error.message);
            el('crawlStop').disabled = false;
        }
    });

    el('crawlExport').addEventListener('click', () => {
        const cell = value => '"' + String(value ?? '').replace(/^[=+@\-]/, "'$&").replaceAll('"', '""') + '"';
        const csv = [
            ['Thời gian', 'Nội dung', 'Tác giả', 'Group', 'URL'],
            ...rows.map(x => [
                x.postedAt || x.createTime || x.createdAt || x.time || '',
                getPostText(x),
                x.author?.name || x.user?.name || x.authorName || '',
                x.groupName || '',
                getPostUrl(x)
            ])
        ];
        const url = URL.createObjectURL(new Blob(['\uFEFF' + csv.map(r => r.map(cell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' }));
        const a = document.createElement('a');
        a.href = url;
        a.download = 'facebook-bat-dong-san.csv';
        a.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    });

    if (runId) {
        setBusy(true);
        poll();
    }
})();
