/**
 * Переключение вкладок «Группировка» / «Скиннинг» / «Позинг».
 * Панель одна, поэтому табы и их содержимое переключаются синхронно.
 * Заодно показываются только те инструменты тулбара, что нужны вкладке.
 */
(function (SkinTool) {
  'use strict';

  function bind() {
    document.querySelectorAll('.tab').forEach((tab) => {
      tab.addEventListener('click', () => switchTab(tab.dataset.tab));
    });
  }

  /** @param {string} name — 'grouping' | 'skinning' | 'pose' */
  function switchTab(name) {
    SkinTool.model.state.currentTab = name;

    document.querySelectorAll('.tab').forEach((tab) => {
      tab.classList.toggle('active', tab.dataset.tab === name);
    });

    document.querySelectorAll('.tab-content').forEach((content) => {
      content.classList.toggle('active', content.dataset.tab === name);
    });

    // Тулбар: на каждой вкладке свой набор кнопок.
    document.querySelectorAll('[data-tab-tools]').forEach((group) => {
      group.classList.toggle('active', group.dataset.tabTools === name);
    });

    if (name === 'skinning') {
      SkinTool.skinning.updateTab();
    } else if (name === 'pose') {
      SkinTool.pose.refresh();
    }
  }

  SkinTool.tabs = { bind, switchTab };
})(window.SkinTool);