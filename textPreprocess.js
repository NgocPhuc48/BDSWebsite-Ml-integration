// textPreprocess.js
// Tiền xử lý nội dung bài viết Facebook rồi xuất ra file .txt (mỗi bài một dòng).

export const DEFAULT_OPTIONS = {
  lowercase: false,        // true: chuyển toàn bộ về chữ thường
  removeUrls: true,        // xóa đường link
  removeEmoji: true,       // xóa emoji / biểu tượng
  removeHashtagSymbol: true, // "#nhadat" -> "nhadat"
  removePhones: false,     // true: xóa số điện thoại
  removeNoise: true,       // xóa "Xem thêm", "See more"...
  minLength: 20            // bỏ bài sau xử lý ngắn hơn số ký tự này
};

const URL_RE = /(https?:\/\/|www\.)\S+/gi;
const EMOJI_RE = /[\p{Extended_Pictographic}\p{Emoji_Presentation}\uFE0E\uFE0F\u200B-\u200D\u2060\uFEFF]/gu;
const PHONE_RE = /(\+?84|0)[\s.\-]?\d{2,3}[\s.\-]?\d{3}[\s.\-]?\d{3,4}\b/g;
const NOISE_RE = /(\.{2,}|…)?\s*(xem thêm|see more|ẩn bớt|see less)\s*$/gi;

export const preprocessText = (raw, opts = DEFAULT_OPTIONS) => {
  let t = String(raw ?? '').normalize('NFC');

  if (opts.removeUrls) t = t.replace(URL_RE, ' ');
  if (opts.removePhones) t = t.replace(PHONE_RE, ' ');
  if (opts.removeEmoji) t = t.replace(EMOJI_RE, ' ');
  if (opts.removeHashtagSymbol) t = t.replace(/#(?=\p{L}|\d)/gu, '');
  if (opts.removeNoise) t = t.replace(NOISE_RE, '');

  // Gom ký tự lặp: "!!!!!" -> "!", "-----" -> "-", "....." -> "..."
  t = t.replace(/([!?\-_=*~+|])\1{1,}/g, '$1').replace(/\.{4,}/g, '...');

  // Xuống dòng / tab / nhiều khoảng trắng -> 1 khoảng trắng (để mỗi bài nằm trên 1 dòng)
  t = t.replace(/[\r\n\t]+/g, ' ').replace(/\s{2,}/g, ' ').trim();

  if (opts.lowercase) t = t.toLocaleLowerCase('vi-VN');
  return t;
};

// Trả về nội dung file .txt: mỗi bài viết đã xử lý nằm trên 1 dòng, bỏ bài rỗng/quá ngắn/trùng
export const buildTxtContent = (posts, opts = DEFAULT_OPTIONS) => {
  const seen = new Set();
  const lines = [];
  posts.forEach((p) => {
    const cleaned = preprocessText(p.text, opts);
    if (cleaned.length < opts.minLength) return;
    const key = cleaned.toLocaleLowerCase('vi-VN');
    if (seen.has(key)) return;
    seen.add(key);
    lines.push(cleaned);
  });
  return lines.join('\n');
};

export const downloadTxt = (content, fileName) => {
  const blob = new Blob(['\uFEFF' + content], { type: 'text/plain;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
};
