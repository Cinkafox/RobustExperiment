/**
 * Переключение вкладок «Группировка» / «Скиннинг» / «Позинг» / «Анимации».
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

  /** @param {string} name — 'grouping' | 'skinning' | 'pose' | 'animation' */
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

    // Док таймлайна занимает место только на своей вкладке.
    const dock = document.getElementById('timelineDock');
    if (dock) dock.classList.toggle('active', name === 'animation');

    if (name === 'skinning') {
      SkinTool.skinning.updateTab();
    } else if (name === 'pose') {
      SkinTool.pose.refresh();
    } else if (name === 'animation') {
      SkinTool.animationUI.refresh();
    } else {
      // Уходим с анимаций — сцену возвращаем в bind-позу, иначе она
      // осталась бы в последнем кадре и мешала правке костей.
      SkinTool.animationUI.stop();
      SkinTool.animationUI.showBindPose();
    }
  }

  SkinTool.tabs = { bind, switchTab };
})(window.SkinTool);