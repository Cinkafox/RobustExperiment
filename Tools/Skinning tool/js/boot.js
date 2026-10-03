/**
 * Загрузчик Three.js с CDN.
 *
 * Three.js и OrbitControls поставляются как ES-модули, а остальной код
 * инструмента написан обычными скриптами (чтобы инструмент продолжал
 * открываться двойным кликом по файлу, без локального сервера).
 * Этот модуль импортирует библиотеки и публикует их в глобальной области,
 * после чего сообщает об этом через событие "skin-tool:ready".
 */
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

window.THREE = THREE;
window.OrbitControls = OrbitControls;

window.dispatchEvent(new Event('skin-tool:ready'));