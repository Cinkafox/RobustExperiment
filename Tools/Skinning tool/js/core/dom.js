/**
 * Мелкие помощники для работы с DOM.
 */
(function (SkinTool) {
  'use strict';

  /**
   * @param {string} id
   * @returns {HTMLElement}
   */
  function $(id) {
    return document.getElementById(id);
  }

  /**
   * Пишет текст в строку статуса под вьюпортом.
   * @param {string} message
   */
  function setStatus(message) {
    $('status').textContent = message;
  }

  /**
   * Превращает блок в зону загрузки файла: клик открывает диалог,
   * файл можно перетащить мышью.
   *
   * @param {string} dropZoneId — id блока-зоны
   * @param {string} inputId — id скрытого <input type="file">
   * @param {(file: File) => void} onFile
   */
  function bindDropZone(dropZoneId, inputId, onFile) {
    const dropZone = $(dropZoneId);
    const fileInput = $(inputId);

    dropZone.addEventListener('click', () => fileInput.click());

    dropZone.addEventListener('dragover', (event) => {
      event.preventDefault();
      dropZone.classList.add('dragover');
    });

    dropZone.addEventListener('dragleave', () => {
      dropZone.classList.remove('dragover');
    });

    dropZone.addEventListener('drop', (event) => {
      event.preventDefault();
      dropZone.classList.remove('dragover');
      const file = event.dataTransfer.files[0];
      if (file) onFile(file);
    });

    fileInput.addEventListener('change', (event) => {
      const file = event.target.files[0];
      if (file) onFile(file);
    });
  }

  SkinTool.dom = { $, setStatus, bindDropZone };
})(window.SkinTool);