/**
 * Waldau Castle — main interactions
 */
(function () {
  'use strict';

  const header = document.getElementById('site-header');
  const burger = document.getElementById('burger');
  const nav = document.getElementById('site-nav');
  const overlay = document.getElementById('nav-overlay');
  const modal = document.getElementById('booking-modal');
  const formWrap = document.getElementById('booking-form-wrap');
  const successWrap = document.getElementById('booking-success');
  const bookingForm = document.getElementById('booking-form');
  const tourIdInput = document.getElementById('booking-tour-id');
  const tourNameInput = document.getElementById('booking-tour-name');
  const tourLabel = document.getElementById('booking-selected-tour');
  const dateInput = document.getElementById('booking-date');

  const PHONE_PREFIX = '+7';
  const PHONE_DIGITS_LEN = 10;

  // Ensure modal is closed on load (CSS must not override [hidden])
  if (modal) {
    modal.hidden = true;
    modal.classList.remove('is-open');
    document.body.style.overflow = '';
  }

  // Min date for booking
  if (dateInput) {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    dateInput.min = tomorrow.toISOString().split('T')[0];
  }

  // --- Phone inputs (+7, digits only, max 10 after code) ---
  function extractSubscriberDigits(value) {
    let digits = String(value).replace(/\D/g, '');
    if (digits.startsWith('7') || digits.startsWith('8')) {
      digits = digits.slice(1);
    }
    return digits.slice(0, PHONE_DIGITS_LEN);
  }

  function formatPhoneDisplay(digits) {
    let result = PHONE_PREFIX;
    if (!digits.length) return result + ' ';

    result += ' (';
    if (digits.length <= 3) {
      return result + digits;
    }
    result += digits.slice(0, 3) + ') ';

    if (digits.length <= 6) {
      return result + digits.slice(3);
    }
    result += digits.slice(3, 6) + '-';

    if (digits.length <= 8) {
      return result + digits.slice(6);
    }
    return result + digits.slice(6, 8) + '-' + digits.slice(8, 10);
  }

  function getPrefixEnd() {
    return PHONE_PREFIX.length + 1;
  }

  function initPhoneInput(input) {
    if (!input || input.dataset.phoneInit === '1') return;
    input.dataset.phoneInit = '1';
    input.setAttribute('inputmode', 'numeric');
    input.setAttribute('autocomplete', 'tel-national');

    function applyDigits(digits) {
      input.dataset.phoneDigits = digits;
      input.value = formatPhoneDisplay(digits);
    }

    input.addEventListener('focus', () => {
      if (!extractSubscriberDigits(input.value).length) {
        applyDigits('');
      }
    });

    input.addEventListener('input', () => {
      applyDigits(extractSubscriberDigits(input.value));
    });

    input.addEventListener('keydown', (e) => {
      const start = input.selectionStart ?? 0;
      const end = input.selectionEnd ?? 0;
      const prefixEnd = getPrefixEnd();

      if (e.key === 'Backspace' && start <= prefixEnd && end <= prefixEnd) {
        e.preventDefault();
        return;
      }
      if (e.key === 'Delete' && start < prefixEnd) {
        e.preventDefault();
        return;
      }
      if (e.key.length === 1 && !/\d/.test(e.key)) {
        e.preventDefault();
      }
    });

    input.addEventListener('paste', (e) => {
      e.preventDefault();
      const pasted = (e.clipboardData || window.clipboardData).getData('text');
      const current = extractSubscriberDigits(input.value);
      const pastedDigits = extractSubscriberDigits(pasted);
      applyDigits((current + pastedDigits).slice(0, PHONE_DIGITS_LEN));
    });

    input.addEventListener('blur', () => {
      const digits = extractSubscriberDigits(input.value);
      if (!digits.length) {
        input.value = '';
        input.dataset.phoneDigits = '';
      }
    });
  }

  function validatePhoneInput(input) {
    const digits = input.dataset.phoneDigits || extractSubscriberDigits(input.value);
    return digits.length === PHONE_DIGITS_LEN;
  }

  function phoneValueForSubmit(input) {
    const digits = input.dataset.phoneDigits || extractSubscriberDigits(input.value);
    return PHONE_PREFIX + digits;
  }

  function getPhoneDigits(input) {
    return input.dataset.phoneDigits || extractSubscriberDigits(input.value);
  }

  function updatePhoneHint(input, hintEl) {
    if (!hintEl) return;
    const digits = getPhoneDigits(input);
    const incomplete = digits.length > 0 && digits.length < PHONE_DIGITS_LEN;
    hintEl.textContent = incomplete
      ? 'Введите 10 цифр номера после +7'
      : '+7 и 10 цифр номера';
    hintEl.classList.toggle('is-error', incomplete);
  }

  function setFieldError(span, message) {
    if (!span) return;
    span.textContent = message;
    span.classList.remove('field-validation-valid');
    span.classList.add('field-validation-error');
  }

  function clearFormErrors(form) {
    form.querySelectorAll('[data-valmsg-for]').forEach((el) => {
      el.textContent = '';
      el.classList.add('field-validation-valid');
      el.classList.remove('field-validation-error');
    });
    form.querySelectorAll('.validation-summary').forEach((el) => {
      el.textContent = '';
      el.hidden = true;
    });
    const hint = document.getElementById('booking-phone-hint');
    if (hint) hint.hidden = false;
  }

  function applyFormErrors(form, errors) {
    if (!errors) return;
    Object.entries(errors).forEach(([field, messages]) => {
      const span = form.querySelector(`[data-valmsg-for="${field}"]`);
      if (span && messages?.length) {
        setFieldError(span, messages[0]);
        if (field === 'Phone') {
          const hint = document.getElementById('booking-phone-hint');
          if (hint) hint.hidden = true;
        }
      }
    });
  }

  function validateBookingForm(form) {
    let valid = true;

    const nameInput = form.querySelector('#booking-name');
    if (!nameInput?.value.trim()) {
      setFieldError(form.querySelector('[data-valmsg-for="FullName"]'), 'Укажите ФИО');
      valid = false;
    }

    const phoneInput = form.querySelector('#booking-phone');
    const phoneError = form.querySelector('[data-valmsg-for="Phone"]');
    const phoneHint = document.getElementById('booking-phone-hint');
    const digits = phoneInput ? getPhoneDigits(phoneInput) : '';

    if (digits.length === 0) {
      setFieldError(phoneError, 'Укажите телефон');
      if (phoneHint) phoneHint.hidden = true;
      valid = false;
    } else if (digits.length < PHONE_DIGITS_LEN) {
      setFieldError(phoneError, 'Введите 10 цифр номера после +7');
      if (phoneHint) phoneHint.hidden = true;
      valid = false;
    }

    const dateInput = form.querySelector('#booking-date');
    if (!dateInput?.value) {
      setFieldError(form.querySelector('[data-valmsg-for="VisitDate"]'), 'Выберите дату визита');
      valid = false;
    }

    const consentInput = form.querySelector('input[name="PersonalDataConsent"]');
    if (!consentInput?.checked) {
      setFieldError(
        form.querySelector('[data-valmsg-for="PersonalDataConsent"]'),
        'Необходимо дать согласие на обработку персональных данных'
      );
      valid = false;
    }

    return valid;
  }

  function setSubmitLoading(button, loading) {
    if (!button) return;
    const spinner = button.querySelector('.btn__spinner');
    button.disabled = loading;
    button.classList.toggle('is-loading', loading);
    if (spinner) spinner.hidden = !loading;
  }

  function resetBookingFormState() {
    if (!bookingForm) return;
    clearFormErrors(bookingForm);
    setSubmitLoading(document.getElementById('booking-submit'), false);
    const hint = document.getElementById('booking-phone-hint');
    if (hint) {
      hint.textContent = '+7 и 10 цифр номера';
      hint.classList.remove('is-error');
      hint.hidden = false;
    }
  }

  function disableBookingFormUnobtrusive() {
    if (!bookingForm || !window.jQuery?.fn?.validate) return;
    const $form = window.jQuery(bookingForm);
    if ($form.data('validator')) {
      $form.data('validator').destroy();
      $form.removeData('validator');
    }
    $form.removeData('unobtrusiveValidation');
  }

  document.addEventListener('DOMContentLoaded', disableBookingFormUnobtrusive);

  document.querySelectorAll('input[type="tel"], .js-phone-input').forEach(initPhoneInput);

  if (bookingForm) {
    const phoneInput = bookingForm.querySelector('input[type="tel"], .js-phone-input');
    const phoneHint = document.getElementById('booking-phone-hint');
    const submitBtn = document.getElementById('booking-submit');

    if (phoneInput) {
      phoneInput.addEventListener('input', () => {
        updatePhoneHint(phoneInput, phoneHint);
        const phoneError = bookingForm.querySelector('[data-valmsg-for="Phone"]');
        if (phoneError?.classList.contains('field-validation-error')) {
          phoneError.textContent = '';
          phoneError.classList.add('field-validation-valid');
          phoneError.classList.remove('field-validation-error');
          if (phoneHint) phoneHint.hidden = false;
        }
      });
      phoneInput.addEventListener('blur', () => updatePhoneHint(phoneInput, phoneHint));
    }

    const consentInput = bookingForm.querySelector('input[name="PersonalDataConsent"]');
    consentInput?.addEventListener('change', () => {
      const consentError = bookingForm.querySelector('[data-valmsg-for="PersonalDataConsent"]');
      if (consentInput.checked && consentError) {
        consentError.textContent = '';
        consentError.classList.add('field-validation-valid');
        consentError.classList.remove('field-validation-error');
      }
    });

    bookingForm.addEventListener(
      'submit',
      async (e) => {
        e.preventDefault();
        e.stopImmediatePropagation();

        clearFormErrors(bookingForm);

        if (!validateBookingForm(bookingForm)) {
          return;
        }

        if (phoneInput) {
          phoneInput.value = phoneValueForSubmit(phoneInput);
        }

        setSubmitLoading(submitBtn, true);

        try {
          const response = await fetch(bookingForm.action, {
            method: 'POST',
            body: new FormData(bookingForm),
            headers: {
              'X-Booking-Modal': '1',
              Accept: 'application/json',
            },
          });

          if (response.ok) {
            bookingForm.reset();
            resetBookingFormState();
            showBookingSuccess();
            return;
          }

          const payload = await response.json().catch(() => null);
          applyFormErrors(bookingForm, payload?.errors);
        } catch {
          const summary = bookingForm.querySelector('.validation-summary');
          if (summary) {
            summary.hidden = false;
            summary.textContent = 'Не удалось отправить заявку. Попробуйте ещё раз.';
          }
        } finally {
          setSubmitLoading(submitBtn, false);
          if (phoneInput && phoneInput.dataset.phoneInit === '1') {
            const digits = getPhoneDigits(phoneInput);
            if (digits.length) {
              phoneInput.value = formatPhoneDisplay(digits);
            }
          }
        }
      },
      true
    );
  }

  // Sticky header scroll state
  function onScroll() {
    if (!header) return;
    header.classList.toggle('is-scrolled', window.scrollY > 40);
  }

  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  // Mobile nav
  function closeNav() {
    burger?.setAttribute('aria-expanded', 'false');
    nav?.classList.remove('is-open');
    overlay?.classList.remove('is-visible');
    if (overlay) overlay.hidden = true;
    if (!modal || modal.hidden) {
      document.body.style.overflow = '';
    }
  }

  function openNav() {
    burger?.setAttribute('aria-expanded', 'true');
    nav?.classList.add('is-open');
    if (overlay) {
      overlay.hidden = false;
      requestAnimationFrame(() => overlay.classList.add('is-visible'));
    }
    document.body.style.overflow = 'hidden';
  }

  burger?.addEventListener('click', () => {
    const isOpen = burger.getAttribute('aria-expanded') === 'true';
    isOpen ? closeNav() : openNav();
  });

  overlay?.addEventListener('click', closeNav);

  nav?.querySelectorAll('a').forEach((link) => {
    link.addEventListener('click', closeNav);
  });

  // Booking modal
  function resetBookingView() {
    formWrap?.removeAttribute('hidden');
    successWrap?.setAttribute('hidden', '');
    resetBookingFormState();
  }

  function openBooking(tourId, tourName) {
    if (!modal) return;
    closeNav();
    resetBookingView();
    if (tourIdInput) tourIdInput.value = tourId || '';
    if (tourNameInput) tourNameInput.value = tourName || '';
    if (tourLabel) {
      if (tourName) {
        tourLabel.textContent = 'Экскурсия: ' + tourName;
        tourLabel.hidden = false;
      } else {
        tourLabel.hidden = true;
      }
    }
    modal.hidden = false;
    modal.classList.add('is-open');
    document.body.style.overflow = 'hidden';
    const phoneInput = modal.querySelector('input[type="tel"], .js-phone-input');
    if (phoneInput) initPhoneInput(phoneInput);
    document.getElementById('booking-name')?.focus();
  }

  function closeBooking() {
    if (!modal) return;
    modal.hidden = true;
    modal.classList.remove('is-open');
    document.body.style.overflow = '';
    resetBookingView();
  }

  function showBookingSuccess() {
    if (!modal) return;
    modal.hidden = false;
    modal.classList.add('is-open');
    formWrap?.setAttribute('hidden', '');
    successWrap?.removeAttribute('hidden');
    document.body.style.overflow = 'hidden';
  }

  document.addEventListener('click', (e) => {
    const openBtn = e.target.closest('[data-booking-open]');
    if (openBtn) {
      e.preventDefault();
      e.stopPropagation();
      const id = openBtn.getAttribute('data-tour-id');
      const name = openBtn.getAttribute('data-tour-name');
      openBooking(id, name);
    }
  });

  // Закрытие — на самом модальном окне (крестик внутри панели не должен терять всплытие)
  modal?.addEventListener('click', (e) => {
    if (modal.hidden) return;
    if (e.target.closest('[data-booking-close]')) {
      e.preventDefault();
      closeBooking();
    }
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      if (modal && !modal.hidden) {
        closeBooking();
      } else {
        closeNav();
      }
    }
  });

  // Scroll reveal
  const revealEls = document.querySelectorAll('.reveal');
  if (revealEls.length && 'IntersectionObserver' in window) {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { rootMargin: '0px 0px -8% 0px', threshold: 0.08 }
    );
    revealEls.forEach((el) => observer.observe(el));
  } else {
    revealEls.forEach((el) => el.classList.add('is-visible'));
  }

  // Hero parallax
  const parallaxBg = document.querySelector('[data-parallax]');
  if (parallaxBg && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    window.addEventListener(
      'scroll',
      () => {
        const y = window.scrollY;
        if (y < window.innerHeight) {
          parallaxBg.style.backgroundPosition = 'center ' + y * 0.35 + 'px';
        }
      },
      { passive: true }
    );
  }

  window.WaldauApp = {
    openBooking,
    closeBooking,
    showBookingSuccess,
  };
})();
