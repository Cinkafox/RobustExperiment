/**
 * Минимальный блочный YAML-парсер без внешних библиотек.
 *
 * Поддерживает всё, что встречается в файлах-прототипах движка:
 *   - несколько документов через "---";
 *   - BOM в начале файла;
 *   - отображения и списки с произвольным отступом, в том числе список
 *     на уровне своего ключа ("components:" + "- type: ...");
 *   - скаляры в кавычках, потоковые списки "[a, b]", комментарии "#";
 *   - теги и якоря вида "!type:BoxShape" (остаются обычными значениями).
 *
 * Скаляры всегда возвращаются строками — доменному коду удобнее
 * разбирать "0,0,0" через split(','), а JSON-совместимость не нужна.
 */
(function (SkinTool) {
  'use strict';

  const DOCUMENT_SEPARATOR = /^---$/;

  /**
   * @param {string} text
   * @returns {Array<object|Array|string>} список документов файла
   */
  function parse(text) {
    return splitDocuments(text)
      .map(parseDocument)
      .filter((document) => document !== null && document !== '');
  }

  /** Делит файл на документы по строкам "---" в нулевой позиции. */
  function splitDocuments(text) {
    const lines = String(text)
      .replace(/^\uFEFF/, '')
      .split(/\r\n|\r|\n/);

    const documents = [[]];

    lines.forEach((line) => {
      if (DOCUMENT_SEPARATOR.test(line.trim())) {
        documents.push([]);
        return;
      }
      documents[documents.length - 1].push(line);
    });

    return documents;
  }

  /** Превращает строки документа в токены {indent, isItem, content}. */
  function tokenize(lines) {
    const tokens = [];

    lines.forEach((rawLine) => {
      const line = stripComment(rawLine);
      if (!line.trim()) return;

      const body = line.trim();
      const isItem = body === '-' || body.startsWith('- ');

      tokens.push({
        indent: line.length - line.trimStart().length,
        isItem,
        content: isItem ? body.substring(1).trim() : body
      });
    });

    return tokens;
  }

  /** Убирает комментарий, не задевая решётку внутри кавычек. */
  function stripComment(line) {
    let inSingle = false;
    let inDouble = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];

      if (char === '"' && !inSingle) {
        inDouble = !inDouble;
      } else if (char === "'" && !inDouble) {
        inSingle = !inSingle;
      } else if (char === '#' && !inSingle && !inDouble && (i === 0 || /\s/.test(line[i - 1]))) {
        return line.substring(0, i);
      }
    }

    return line;
  }

  /**
   * Делит строку на ключ и значение по первому двоеточию, за которым
   * идёт пробел или конец строки (признак YAML-ключа).
   *
   * @returns {{key: string, value: string}|null}
   */
  function splitKeyValue(content) {
    let inSingle = false;
    let inDouble = false;

    for (let i = 0; i < content.length; i++) {
      const char = content[i];

      if (char === '"' && !inSingle) {
        inDouble = !inDouble;
        continue;
      }
      if (char === "'" && !inDouble) {
        inSingle = !inSingle;
        continue;
      }
      if (char === ':' && !inSingle && !inDouble) {
        if (i + 1 >= content.length || content[i + 1] === ' ') {
          return {
            key: unquote(content.substring(0, i).trim()),
            value: content.substring(i + 1).trim()
          };
        }
      }
    }

    return null;
  }

  /** Значение: потоковый список разбивается на массив, кавычки снимаются. */
  function parseScalar(raw) {
    const value = raw.trim();

    if (value.startsWith('[') && value.endsWith(']')) {
      return value
        .substring(1, value.length - 1)
        .split(',')
        .map((part) => unquote(part.trim()))
        .filter((part) => part.length > 0);
    }

    return unquote(value);
  }

  function unquote(value) {
    if (value.length >= 2) {
      const first = value[0];
      const last = value[value.length - 1];
      if ((first === '"' && last === '"') || (first === "'" && last === "'")) {
        return value.substring(1, value.length - 1);
      }
    }

    return value;
  }

  /** Разбирает один документ; возвращает корень (объект, список или строку). */
  function parseDocument(lines) {
    const tokens = tokenize(lines);
    let index = 0;

    function parseBlock(indent) {
      const first = tokens[index];
      if (!first || first.indent < indent) return null;
      return first.isItem ? parseSequence(first.indent) : parseMapping(first.indent);
    }

    /**
     * Вложенный блок после "ключ:". Список может начинаться с тем же
     * отступом, что и сам ключ, — так его пишут в файлах прототипов.
     */
    function parseChildBlock(parentIndent) {
      const next = tokens[index];
      if (!next) return '';

      if (next.isItem && next.indent >= parentIndent) return parseSequence(next.indent);
      if (next.indent > parentIndent) return parseBlock(next.indent);

      return '';
    }

    function parseMapping(indent) {
      const result = {};

      while (index < tokens.length) {
        const token = tokens[index];

        if (token.indent < indent) break;
        if (token.indent > indent || token.isItem) break;

        const pair = splitKeyValue(token.content);
        if (!pair) {
          // Строка без "ключ: значение" (например "!type:BoxShape").
          const start = index;
          result[token.content] = parseChildBlock(indent);
          if (index === start) index++;
          continue;
        }

        index++;
        result[pair.key] = pair.value === '' ? parseChildBlock(indent) : parseScalar(pair.value);
      }

      return result;
    }

    function parseSequence(indent) {
      const list = [];

      while (index < tokens.length) {
        const token = tokens[index];

        if (token.indent < indent) break;
        if (!token.isItem) break;
        if (token.indent > indent) {
          index++;
          continue;
        }

        // Отступ вложенных ключей элемента берём у следующей строки:
        // обычно это indent + 2, но встречаются и другие значения.
        const next = tokens[index + 1];
        const innerIndent = next && next.indent > indent ? next.indent : indent + 2;
        index++;

        if (token.content === '') {
          list.push(parseChildBlock(indent));
        } else if (splitKeyValue(token.content)) {
          list.push(parseInlineMapping(indent, token.content, innerIndent));
        } else {
          list.push(parseScalar(token.content));
        }
      }

      return list;
    }

    /** Элемент списка, у которого первая пара "ключ: значение" — на строке "- ". */
    function parseInlineMapping(dashIndent, firstContent, innerIndent) {
      const result = {};
      const firstPair = splitKeyValue(firstContent);

      result[firstPair.key] = firstPair.value === ''
        ? parseChildBlock(dashIndent)
        : parseScalar(firstPair.value);

      while (index < tokens.length) {
        const token = tokens[index];

        if (token.indent < innerIndent) break;
        if (token.isItem || token.indent > innerIndent) break;

        const pair = splitKeyValue(token.content);
        if (!pair) {
          const start = index;
          result[token.content] = parseChildBlock(innerIndent);
          if (index === start) index++;
          continue;
        }

        index++;
        result[pair.key] = pair.value === '' ? parseChildBlock(innerIndent) : parseScalar(pair.value);
      }

      return result;
    }

    if (!tokens.length) return null;
    return parseBlock(tokens[0].indent);
  }

  SkinTool.yamlParser = { parse };
})(window.SkinTool);