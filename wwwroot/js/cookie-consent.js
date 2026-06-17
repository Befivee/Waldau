(function () {
  'use strict';

  var STORAGE_KEY = 'waldau_cookie_consent_v1';
  var banner = document.getElementById('cookie-consent');
  if (!banner) return;

  var gaId = (banner.dataset.gaId || '').trim();
  var ymId = (banner.dataset.ymId || '').trim();
  var hasAnalytics = Boolean(gaId || ymId);

  function grantGoogleAnalytics() {
    if (!gaId || typeof window.gtag !== 'function') return;
    window.gtag('consent', 'update', {
      analytics_storage: 'granted'
    });
  }

  function denyGoogleAnalytics() {
    if (!gaId || typeof window.gtag !== 'function') return;
    window.gtag('consent', 'update', {
      analytics_storage: 'denied'
    });
  }

  function loadYandexMetrika(id) {
    if (!id || window.__waldauYmLoaded) return;
    window.__waldauYmLoaded = true;

    window.ym = window.ym || function () {
      (window.ym.a = window.ym.a || []).push(arguments);
    };
    window.ym.l = Date.now();

    var script = document.createElement('script');
    script.async = true;
    script.src = 'https://mc.yandex.ru/metrika/tag.js';
    document.head.appendChild(script);

    window.ym(parseInt(id, 10), 'init', {
      clickmap: true,
      trackLinks: true,
      accurateTrackBounce: true,
      webvisor: false
    });
  }

  function hideBanner() {
    banner.hidden = true;
    banner.setAttribute('aria-hidden', 'true');
  }

  function applyConsent(consent) {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(consent));
    } catch (_) {
      /* ignore storage errors */
    }

    hideBanner();

    if (consent && consent.analytics) {
      grantGoogleAnalytics();
      loadYandexMetrika(ymId);
    } else {
      denyGoogleAnalytics();
    }
  }

  function readSavedConsent() {
    try {
      var raw = localStorage.getItem(STORAGE_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch (_) {
      return null;
    }
  }

  var saved = readSavedConsent();
  if (saved) {
    if (saved.analytics) {
      grantGoogleAnalytics();
      loadYandexMetrika(ymId);
    } else {
      denyGoogleAnalytics();
    }
    hideBanner();
    return;
  }

  if (!hasAnalytics) {
    applyConsent({ necessary: true, analytics: false, ts: Date.now() });
    return;
  }

  banner.hidden = false;
  banner.removeAttribute('hidden');
  banner.removeAttribute('aria-hidden');

  var acceptAllBtn = banner.querySelector('[data-cookie-accept-all]');
  var acceptNecessaryBtn = banner.querySelector('[data-cookie-accept-necessary]');

  if (acceptAllBtn) {
    acceptAllBtn.addEventListener('click', function () {
      applyConsent({ necessary: true, analytics: true, ts: Date.now() });
    });
  }

  if (acceptNecessaryBtn) {
    acceptNecessaryBtn.addEventListener('click', function () {
      applyConsent({ necessary: true, analytics: false, ts: Date.now() });
    });
  }
})();
