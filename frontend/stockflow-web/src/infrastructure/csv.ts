const numericLiteral = /^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?$/u
const formulaPrefix = /^(?:[\t\r\n]|[\s\u0000-\u001F\u007F-\u009F\uFEFF]*[=+\-@＝＋－＠])/u

function escapeCsvCell(value: unknown): string {
  let text = value == null ? '' : String(value)

  // Keep actual numeric values (including negative quantities) numeric in spreadsheet apps.
  if (!numericLiteral.test(text) && formulaPrefix.test(text)) text = `'${text}`

  return `"${text.replaceAll('"', '""')}"`
}

export function serializeCsv(rows: ReadonlyArray<ReadonlyArray<unknown>>): string {
  return rows.map((row) => row.map(escapeCsvCell).join(',')).join('\n')
}
