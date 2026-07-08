/** @template T */
export function pickRandom(items) {
  if (!items?.length) {
    return undefined;
  }
  return items[Math.floor(Math.random() * items.length)];
}

/** Как RequestService.GenerateRequestNumber в WinUI. */
export function generateRequestNumber() {
  const suffix = Math.floor(1000 + Math.random() * 9000);
  const stamp = new Date()
    .toISOString()
    .replace(/[-:TZ.]/g, "")
    .slice(0, 14);
  return `З-${stamp}-${suffix}`;
}
