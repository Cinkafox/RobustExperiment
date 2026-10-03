/**
 * Пространство имён, общее для всех модулей инструмента.
 *
 * Каждый модуль — отдельный файл, который вешает свой API на объект
 * window.SkinTool: `SkinTool.<модуль> = { ... }`.
 *
 * Здесь же живут ленивые ссылки на Three.js, чтобы модули могли
 * обращаться к библиотеке в любой момент, независимо от порядка загрузки.
 */
(function (global) {
  'use strict';

  const SkinTool = global.SkinTool || {};
  global.SkinTool = SkinTool;

  Object.defineProperty(SkinTool, 'THREE', {
    get: () => global.THREE
  });

  Object.defineProperty(SkinTool, 'OrbitControls', {
    get: () => global.OrbitControls
  });

  /**
   * Вызывает fn как только Three.js будет загружен.
   * @param {Function} fn
   */
  SkinTool.onReady = function (fn) {
    if (global.THREE) {
      fn();
    } else {
      global.addEventListener('skin-tool:ready', fn, { once: true });
    }
  };
})(window);