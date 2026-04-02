const CH = ['không', 'một', 'hai', 'ba', 'bốn', 'năm', 'sáu', 'bảy', 'tám', 'chín'];

/** 1–99 (không có hàng trăm). */
function readSubHundred(n: number): string {
  const chuc = Math.floor(n / 10);
  const dv = n % 10;
  if (chuc === 0) return CH[dv];
  if (chuc === 1) {
    if (dv === 0) return 'mười';
    if (dv === 1) return 'mười một';
    if (dv === 5) return 'mười lăm';
    return `mười ${CH[dv]}`;
  }
  let s = `${CH[chuc]} mươi`;
  if (dv === 1) s += ' mốt';
  else if (dv === 5) s += ' lăm';
  else if (dv > 0) s += ` ${CH[dv]}`;
  return s;
}

/**
 * Đọc nhóm 0–999.
 * @param afterHigher — đã có nhóm lớn hơn bên trái (vd. phần sau “triệu”)
 */
function readTriple(n: number, afterHigher: boolean): string {
  if (n === 0) return '';
  if (afterHigher && n < 100) {
    const chuc = Math.floor(n / 10);
    const dv = n % 10;
    if (chuc === 0) return `lẻ ${CH[dv]}`;
    return readSubHundred(n);
  }

  const tram = Math.floor(n / 100);
  const rest = n % 100;
  const parts: string[] = [];
  if (tram > 0) {
    parts.push(`${CH[tram]} trăm`);
  } else if (afterHigher && rest > 0) {
    parts.push('không trăm');
  }
  if (rest > 0) {
    parts.push(readSubHundred(rest));
  }
  return parts.join(' ').replace(/\s+/g, ' ').trim();
}

/**
 * Đọc số tiền VND (nguyên) ra tiếng Việt, kết thúc bằng “đồng”.
 */
export function vndIntegerToVietnameseWords(amount: number): string {
  if (!Number.isFinite(amount) || amount < 0) return '';
  const num = Math.floor(Math.abs(amount));
  if (num === 0) return 'Không đồng';

  const chunks: number[] = [];
  let t = num;
  while (t > 0) {
    chunks.push(t % 1000);
    t = Math.floor(t / 1000);
  }

  const scales = ['', 'nghìn', 'triệu', 'tỷ', 'nghìn tỷ', 'triệu tỷ'];
  const parts: string[] = [];

  for (let i = chunks.length - 1; i >= 0; i--) {
    const v = chunks[i];
    if (v === 0) continue;
    const afterHigher = parts.length > 0;
    const text = readTriple(v, afterHigher);
    parts.push(text + (scales[i] ? ` ${scales[i]}` : ''));
  }

  const s = parts.join(' ').replace(/\s+/g, ' ').trim();
  if (!s) return 'Không đồng';
  return s.charAt(0).toUpperCase() + s.slice(1) + ' đồng';
}
