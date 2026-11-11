/**
 * Comparison key for searching, the same letters as the server's PersianText.Key: Arabic ي/ى/ك = Persian ی/ک,
 * Persian and Arabic digits = 0-9, spaces and half-spaces ignored, Latin letters lower-cased.
 */
export function searchKey(s: string): string {
  return s
    .replace(/[يى]/g, 'ی')
    .replace(/ك/g, 'ک')
    .replace(/[۰-۹]/g, d => String(d.charCodeAt(0) - 0x06f0))
    .replace(/[٠-٩]/g, d => String(d.charCodeAt(0) - 0x0660))
    .replace(/[\s‌]/g, '')
    .toLowerCase()
}
