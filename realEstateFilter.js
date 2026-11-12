// realEstateFilter.js
// Lọc bài viết không liên quan đến mua bán nhà đất / bất động sản.
// Chạy hoàn toàn ở trình duyệt, không cần API, hỗ trợ cả bài viết gõ có dấu lẫn không dấu.
//
// Nguyên tắc (chặt hơn bản cũ):
//  - Chữ "nhà"/"đất" đứng riêng chỉ là tín hiệu YẾU, không đủ để giữ bài.
//  - Phải có từ BĐS CỤ THỂ (căn hộ, đất nền, sổ đỏ, nhà phố...) hoặc
//    từ BĐS yếu kèm đủ bằng chứng (ý định bán/mua + diện tích/giá VNĐ).
//  - Bài có giá bằng $/USD, dụng cụ, cảnh báo lừa đảo... bị loại thẳng.

const stripAccents = (s) =>
  String(s || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/\s+/g, ' ')
    .trim();

const wordsRe = (list) => new RegExp('\\b(' + list.join('|') + ')\\b');

// ---- BĐS mạnh (không dấu): loại hình cụ thể, ít nhầm nghĩa ----
const STRONG_PROPERTY_RE = wordsRe([
  'nha dat', 'nha pho', 'nha cap 4', 'nha rieng', 'nha mat tien', 'nha mat pho', 'nha hem', 'nha ngo',
  'nha \\d+ tang', 'nha \\d+ tret', 'nha 1 tret', 'can ho', 'chung cu', 'penthouse', 'duplex',
  'biet thu', 'lien ke', 'shophouse', 'kiot', 'ki ot', 'dat nen', 'lo dat', 'thua dat', 'dat tho cu',
  'nen dat', 'mat tien', 'mat pho', 'so do', 'so hong', 'bat dong san', 'bds', 'khu do thi', 'kdt',
  'phan lo', 'tach thua', 'condotel', 'officetel', 'nha tro', 'phong tro'
]);

// ---- BĐS mạnh (CÓ dấu): tránh "đặt ở" bị nhầm thành "đất ở" ----
const STRONG_PROPERTY_ACCENT_RE =
  /(^|[^\p{L}])(đất ở|đất vườn|đất thổ cư|đất nông nghiệp|đất dự án|đất mặt tiền|đất nền|nền đất|lô đất|thửa đất|sổ đỏ|sổ hồng)([^\p{L}]|$)/u;

// ---- BĐS yếu: chỉ có giá trị khi đi kèm nhiều bằng chứng khác ----
const WEAK_PROPERTY_ACCENT_RE = /(^|[^\p{L}])(nhà|đất)([^\p{L}]|$)/u;
const WEAK_PROPERTY_RE = wordsRe([
  'mat bang', 'phap ly', 'quy hoach', 'nha moi', 'nha xay', 'homestay'
]);

// ---- Ý định mua bán rõ ràng (đã bỏ các từ chung chung: ban lai, dau tu, giao dich, booking...) ----
const STRONG_TRADE_RE = wordsRe([
  'can ban', 'ban gap', 'ban nha', 'ban dat', 'ban can ho', 'ban chung cu', 'ban lo', 'ban biet thu',
  'ban shophouse', 'ban dat nen', 'ban nhanh', 'ban ngay', 'chao ban', 'nhuong lai', 'cat lo',
  'mua nha', 'mua dat', 'can mua', 'tim mua', 'nhan ky gui', 'ky gui', 'chuyen nhuong', 'sang nhuong',
  'cho thue', 'can thue', 'chinh chu', 'mien trung gian', 'khong qua trung gian',
  'gia chot', 'chot nhanh', 'hoa hong', 'mo ban', 'nhan coc', 'ra hang', 'ngop bank'
]);

// ---- Diện tích, kích thước, giá, phòng ----
const AREA_RE = /\d+([.,]\d+)?\s*(m2|m²|m\^2|met vuong|m vuong|ha\b|hecta)/;
const SIZE_RE = /\b\d+([.,]\d+)?\s*[x*]\s*\d+([.,]\d+)?\s*m?\b/;
const PRICE_RE = /\d+([.,]\d+)?\s*(ty|tr\b|trieu|nghin ty)/;
const ROOM_RE = /\b\d+\s*(pn|phong ngu|wc|toilet|tang|lau|tam)\b/;

// ---- Loại thẳng: giá ngoại tệ, dụng cụ/đồ Mỹ, cảnh báo lừa đảo ----
const FOREIGN_CURRENCY_RE = /\$\s*\d|\d\s*\$|\b(usd|dollars?|do la)\b/;
const HARD_NEGATIVE_RE = wordsRe([
  'nailer', 'stapler', 'porter cable', 'mobile home', 'resale', 'may khoan', 'dung cu',
  'lua dao', 'canh bao', 'scam', 'bi lua', 'mao danh'
]);

// ---- Bài tin tức / cảnh báo / tư vấn / quảng cáo khóa học (KHÔNG phải tin rao) ----
// Chỉ cần 1 dấu hiệu "mạnh" là loại.
const ARTICLE_HARD_RE = wordsRe([
  'phan anh', 'canh giac', 'khoi to', 'cong an', 'toa an', 'vi pham phap luat', 'nghi dinh',
  'luat dat dai', 'luat nha o', 'xu phat', 'tin tuc', 'theo bao', 'theo thong tin', 'cau chuyen',
  'bi quyet', 'khoa hoc', 'hoi thao', 'luat su', 'sang ten', 'tranh bi lua'
]);
// Cần từ 2 dấu hiệu "nhẹ" trở lên mới loại.
const ARTICLE_SOFT_RE = new RegExp('\\b(' + [
  'kinh nghiem', 'can biet', 'tai sao', 'vi sao', 'luu y', 'rui ro', 'phap luat', 'dao tao',
  'anh em dau tu', 'chuyen gia', 'du bao', 'thi truong bat dong san', 'thi truong', 'xu huong',
  'phan tich', 'nhan dinh', 'giao dich phai', 'kiem tra phap ly', 'tiet kiem thue', 'nhieu nguoi'
].join('|') + ')\\b', 'g');
const ARTICLE_ACCENT_RE = /(^|[^\p{L}])(có nên|nên hay không)([^\p{L}]|$)/u; // tránh nhầm "có nền"

// ---- Dấu hiệu liên hệ (tin rao thật gần như luôn có) ----
const PHONE_RE = /(\+?84|0)[\s.\-]?\d{2,3}[\s.\-]?\d{3}[\s.\-]?\d{3,4}\b/;
const CONTACT_WORD_RE = wordsRe(['zalo', 'lien he', 'lh', 'sdt', 'hotline', 'inbox', 'call']);

// ---- Chủ đề lạc đề (trừ điểm) ----
const NEGATIVE_RE = wordsRe([
  'tuyen dung', 'viec lam', 'tuyen nhan vien', 'tuyen sale', 'tuyen ctv', 'tim viec', 'luong cung',
  'xe may', 'xe tai', 'xe dap', 'bien so', 'ban xe', 'mua xe', 'sh mode', 'vision',
  'dien thoai', 'iphone', 'samsung', 'laptop', 'macbook', 'may tinh', 'tai khoan', 'acc game', 'game',
  'quan ao', 'giay dep', 'tui xach', 'my pham', 'thoi trang', 'thuc pham chuc nang', 'nuoc hoa',
  'vay tien', 'vay von', 'cho vay', 'vay nong', 'tin dung den', 'bao no xau',
  've may bay', 'tour du lich', 'dat ve', 'khach san',
  'da cap', 'kiem tien online', 'tiktok shop', 'livestream', 'ban hang online',
  'thu cung', 'cho meo', 'gia suc', 'cay giong', 'phan bon'
]);

/**
 * Chấm điểm một bài viết.
 * @returns {{ relevant: boolean, score: number, reasons: string[] }}
 */
export const scoreRealEstate = (rawText) => {
  const original = String(rawText || '').normalize('NFC').toLowerCase();
  const text = stripAccents(rawText);
  const reasons = [];

  if (text.length < 15) {
    return { relevant: false, score: 0, reasons: ['Nội dung quá ngắn/trống'] };
  }

  // Loại thẳng
  if (FOREIGN_CURRENCY_RE.test(text)) {
    return { relevant: false, score: -10, reasons: ['giá bằng $/USD (không phải BĐS Việt Nam)'] };
  }
  if (HARD_NEGATIVE_RE.test(text)) {
    return { relevant: false, score: -10, reasons: ['chủ đề lạc đề (dụng cụ/cảnh báo lừa đảo/mobile home...)'] };
  }

  // Quá dài thường là bài báo/bài phân tích, không phải tin rao
  const hasContact = PHONE_RE.test(String(rawText || '')) || CONTACT_WORD_RE.test(text);
  if (text.length > 3000 || (text.length > 1500 && !hasContact)) {
    return { relevant: false, score: -5, reasons: ['bài quá dài, giống bài báo/phân tích'] };
  }
  if (ARTICLE_HARD_RE.test(text) || ARTICLE_ACCENT_RE.test(original)) {
    return { relevant: false, score: -8, reasons: ['bài tin tức/cảnh báo/tư vấn, không phải tin rao'] };
  }
  const softHits = new Set(text.match(ARTICLE_SOFT_RE) || []).size;
  if (softHits >= 2) {
    return { relevant: false, score: -6, reasons: ['bài phân tích/bàn luận thị trường'] };
  }

  const strongProperty = STRONG_PROPERTY_RE.test(text) || STRONG_PROPERTY_ACCENT_RE.test(original);
  const weakProperty = WEAK_PROPERTY_ACCENT_RE.test(original) || WEAK_PROPERTY_RE.test(text);
  const hasTrade = STRONG_TRADE_RE.test(text);
  const hasArea = AREA_RE.test(text) || SIZE_RE.test(text);
  const hasPrice = PRICE_RE.test(text);
  const hasRoom = ROOM_RE.test(text);
  const hasNegative = NEGATIVE_RE.test(text);

  let score = 0;
  if (strongProperty) { score += 3; reasons.push('loại BĐS cụ thể'); }
  else if (weakProperty) { score += 1; reasons.push('từ BĐS chung chung'); }
  if (hasTrade) { score += 2; reasons.push('ý định mua bán'); }
  if (hasArea) { score += 2; reasons.push('diện tích/kích thước'); }
  if (hasPrice) { score += 1; reasons.push('giá VNĐ'); }
  if (hasRoom) { score += 1; reasons.push('phòng/tầng'); }
  if (hasNegative) { score -= 3; reasons.push('chủ đề lạc đề'); }

  const enoughEvidence =
    (strongProperty && (hasTrade || hasArea)) ||
    (weakProperty && hasTrade && (hasArea || hasPrice));

  // Tin rao thật phải có liên hệ, hoặc diện tích kèm giá/ý định bán
  const looksLikeListing = hasContact || (hasArea && (hasPrice || hasTrade));
  if (hasContact) { score += 1; reasons.push('có liên hệ'); }

  const relevant = enoughEvidence && looksLikeListing && score >= 4;
  if (!relevant && !reasons.length) reasons.push('Không liên quan BĐS');
  return { relevant, score, reasons };
};

export const isRealEstatePost = (rawText) => scoreRealEstate(rawText).relevant;

/**
 * Lọc mảng bài viết đã chuẩn hóa (có trường `text`), đồng thời loại trùng postId.
 * @returns {{ kept: any[], removed: any[] }}
 */
export const filterRealEstatePosts = (posts) => {
  const kept = [];
  const removed = [];
  const seen = new Set();
  const seenText = new Set();

  posts.forEach((p) => {
    const dupKey = p.postId;
    if (dupKey && seen.has(dupKey)) { removed.push({ ...p, _reason: 'Trùng bài' }); return; }
    if (dupKey) seen.add(dupKey);

    const contentKey = stripAccents(p.text).replace(/[^a-z0-9 ]/g, '').slice(0, 120);
    if (contentKey.length > 30) {
      if (seenText.has(contentKey)) { removed.push({ ...p, _reason: 'Trùng nội dung' }); return; }
      seenText.add(contentKey);
    }

    const { relevant, reasons } = scoreRealEstate(p.text);
    if (relevant) kept.push(p);
    else removed.push({ ...p, _reason: reasons.join(', ') || 'Không liên quan BĐS' });
  });

  return { kept, removed };
};