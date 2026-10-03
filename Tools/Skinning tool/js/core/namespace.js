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
   * Порядок осей, в котором углы превращаются в поворот.
   *
   * Движок в QuaternionExt.ToQuaternion собирает поворот как qy * qx * qz,
   * и обратное разложение в ToEulerAngle вытаскивает углы в том же порядке.
   * Three.js для 'XYZ' собирает qx * qy * qz — это другой поворот, и разница
   * заметна, как только ненулевых компонент больше одной: у зеркальных костей
   * вроде right-leg кадр (15°, 180°, 0) в 'XYZ' крутит ногу вокруг МИРОВОЙ
   * оси X, и правая нога шагает в такт левой. В 'YXZ' тот же кадр крутит
   * вокруг собственной оси кости, как в игре.
   *
   * Менять это в одном месте нельзя: конверсия должна быть одинаковой при
   * импорте, экспорте, сэмплировании и отрисовке, иначе они разъедутся.
   */
  SkinTool.EULER_ORDER = 'YXZ';

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