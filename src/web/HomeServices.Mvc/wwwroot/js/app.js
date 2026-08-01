/**
 * HomeServices — Premium Application JavaScript
 * Phase 3 Complete UI/UX Rebuild
 * Modules: Toast, Modal, Nav, ScrollReveal, CountUp, LoadingBar, Ripple, Forms, Filters
 */

(function () {
  'use strict';

  /* =========================================================================
     UTILITY HELPERS
     ========================================================================= */

  const $ = (sel, ctx = document) => ctx.querySelector(sel);
  const $$ = (sel, ctx = document) => [...ctx.querySelectorAll(sel)];
  const on = (el, ev, fn, opts) => el && el.addEventListener(ev, fn, opts);
  const off = (el, ev, fn) => el && el.removeEventListener(ev, fn);

  function debounce(fn, delay = 300) {
    let t;
    return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), delay); };
  }

  function clamp(val, min, max) { return Math.min(Math.max(val, min), max); }

  /* =========================================================================
     1. LOADING BAR
     ========================================================================= */

  const LoadingBar = (() => {
    let bar, fill, timer, progress = 0;

    function init() {
      bar = document.createElement('div');
      bar.className = 'loading-bar';
      bar.innerHTML = '<div class="loading-bar__fill"></div>';
      document.body.prepend(bar);
      fill = bar.querySelector('.loading-bar__fill');
    }

    function set(pct) {
      progress = clamp(pct, 0, 100);
      fill.style.width = progress + '%';
      fill.style.opacity = progress > 0 && progress < 100 ? '1' : '0';
    }

    function start() {
      clearInterval(timer);
      set(0);
      fill.style.opacity = '1';
      fill.style.transition = 'width 0.3s ease';
      let current = 0;
      timer = setInterval(() => {
        // Asymptotic approach — slow down near 85%
        const inc = current < 20 ? 8 : current < 50 ? 4 : current < 80 ? 1.5 : 0.3;
        current = Math.min(current + inc, 85);
        set(current);
      }, 120);
    }

    function done() {
      clearInterval(timer);
      fill.style.transition = 'width 0.2s ease, opacity 0.4s ease 0.3s';
      set(100);
      setTimeout(() => { fill.style.opacity = '0'; setTimeout(() => set(0), 400); }, 300);
    }

    function fail() {
      clearInterval(timer);
      fill.style.background = 'var(--error-500)';
      set(100);
      setTimeout(() => { fill.style.opacity = '0'; fill.style.background = ''; setTimeout(() => set(0), 400); }, 600);
    }

    return { init, start, done, fail, set };
  })();

  /* =========================================================================
     2. TOAST NOTIFICATION SYSTEM
     ========================================================================= */

  const Toast = (() => {
    let container;
    const queue = [];
    const DURATION = 4500;

    const ICONS = {
      success: '✓',
      error:   '✕',
      warning: '⚠',
      info:    'ℹ',
    };

    function getContainer() {
      if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        container.setAttribute('role', 'region');
        container.setAttribute('aria-label', 'اعلان‌ها');
        document.body.appendChild(container);
      }
      return container;
    }

    function show(message, type = 'info', options = {}) {
      const { title = '', duration = DURATION, persistent = false } = options;
      const c = getContainer();

      const toast = document.createElement('div');
      toast.className = `toast toast--${type}`;
      toast.setAttribute('role', 'alert');
      toast.setAttribute('aria-live', 'polite');

      const titleHtml = title ? `<div class="toast__title">${title}</div>` : '';
      const progressHtml = !persistent
        ? `<div class="toast__progress"><div class="toast__progress-bar" style="animation-duration:${duration}ms"></div></div>`
        : '';

      toast.innerHTML = `
        <div class="toast__icon">${ICONS[type] || ICONS.info}</div>
        <div class="toast__body">
          ${titleHtml}
          <div class="toast__message">${message}</div>
        </div>
        <button class="toast__close" aria-label="بستن">✕</button>
        ${progressHtml}
      `;

      c.appendChild(toast);

      // Trigger enter animation
      requestAnimationFrame(() => requestAnimationFrame(() => toast.classList.add('show')));

      // Close button
      on(toast.querySelector('.toast__close'), 'click', () => dismiss(toast));

      // Auto dismiss
      let dismissTimer;
      if (!persistent) {
        dismissTimer = setTimeout(() => dismiss(toast), duration);
        // Pause on hover
        on(toast, 'mouseenter', () => clearTimeout(dismissTimer));
        on(toast, 'mouseleave', () => { dismissTimer = setTimeout(() => dismiss(toast), 1500); });
      }

      return toast;
    }

    function dismiss(toast) {
      toast.classList.remove('show');
      toast.classList.add('hide');
      setTimeout(() => toast.remove(), 350);
    }

    function success(msg, opts)  { return show(msg, 'success', opts); }
    function error(msg, opts)    { return show(msg, 'error',   opts); }
    function warning(msg, opts)  { return show(msg, 'warning', opts); }
    function info(msg, opts)     { return show(msg, 'info',    opts); }

    // Process TempData alerts injected into DOM by the server
    function processTempData() {
      $$('[data-toast]').forEach(el => {
        const type = el.dataset.toast || 'info';
        const msg  = el.dataset.message || el.textContent.trim();
        if (msg) show(msg, type, { title: el.dataset.title || '' });
        el.remove();
      });
    }

    return { show, dismiss, success, error, warning, info, processTempData };
  })();

  /* =========================================================================
     3. MODAL SYSTEM
     ========================================================================= */

  const Modal = (() => {
    let backdrop, currentModal;

    function getBackdrop() {
      if (!backdrop) {
        backdrop = document.createElement('div');
        backdrop.className = 'modal-backdrop';
        on(backdrop, 'click', closeAll);
        document.body.appendChild(backdrop);
      }
      return backdrop;
    }

    function open(modalOrId) {
      const modal = typeof modalOrId === 'string'
        ? document.getElementById(modalOrId)
        : modalOrId;

      if (!modal) return;

      const bd = getBackdrop();
      bd.style.display = 'block';
      modal.style.display = 'flex';
      document.body.style.overflow = 'hidden';

      requestAnimationFrame(() => requestAnimationFrame(() => {
        bd.classList.add('show');
        modal.classList.add('show');
      }));

      currentModal = modal;

      // ESC to close
      const onKey = (e) => { if (e.key === 'Escape') closeAll(); };
      on(document, 'keydown', onKey);
      modal._escHandler = onKey;

      // Focus first focusable
      const focusable = modal.querySelector('button, input, select, textarea, a, [tabindex]:not([tabindex="-1"])');
      setTimeout(() => focusable?.focus(), 100);
    }

    function close(modalOrId) {
      const modal = typeof modalOrId === 'string'
        ? document.getElementById(modalOrId)
        : (modalOrId || currentModal);

      if (!modal) return;

      modal.classList.remove('show');
      backdrop?.classList.remove('show');
      document.body.style.overflow = '';

      setTimeout(() => {
        modal.style.display = 'none';
        if (backdrop) backdrop.style.display = 'none';
      }, 300);

      if (modal._escHandler) off(document, 'keydown', modal._escHandler);
      currentModal = null;
    }

    function closeAll() { close(currentModal); }

    // Confirm modal helper
    function confirm(options = {}) {
      const {
        title = 'آیا مطمئن هستید؟',
        message = '',
        confirmText = 'تأیید',
        cancelText = 'انصراف',
        danger = false,
      } = options;

      return new Promise((resolve) => {
        const id = 'modal-confirm-' + Date.now();
        const wrap = document.createElement('div');
        wrap.className = 'modal-wrap';
        wrap.id = id;

        wrap.innerHTML = `
          <div class="modal modal--sm" role="dialog" aria-modal="true" aria-labelledby="${id}-title">
            <div class="modal__header">
              <div class="modal__title" id="${id}-title">${title}</div>
              <button class="modal__close js-modal-close" aria-label="بستن">✕</button>
            </div>
            <div class="modal__body">${message}</div>
            <div class="modal__footer">
              <button class="btn btn-secondary js-modal-cancel">${cancelText}</button>
              <button class="btn ${danger ? 'btn-danger' : 'btn-primary'} js-modal-confirm">${confirmText}</button>
            </div>
          </div>
        `;

        document.body.appendChild(wrap);
        open(wrap);

        on(wrap.querySelector('.js-modal-confirm'), 'click', () => { close(wrap); wrap.remove(); resolve(true); });
        on(wrap.querySelector('.js-modal-cancel'), 'click', () => { close(wrap); wrap.remove(); resolve(false); });
        on(wrap.querySelector('.js-modal-close'), 'click', () => { close(wrap); wrap.remove(); resolve(false); });
      });
    }

    return { open, close, closeAll, confirm };
  })();

  /* =========================================================================
     4. NAVIGATION
     ========================================================================= */

  const Nav = (() => {
    function init() {
      const nav = $('.hs-nav');
      if (!nav) return;

      // Scroll behaviour — add/remove .scrolled class
      const onScroll = debounce(() => {
        nav.classList.toggle('scrolled', window.scrollY > 20);
      }, 10);
      on(window, 'scroll', onScroll, { passive: true });
      onScroll();

      // Mobile drawer toggle
      const menuBtn = $('.hs-nav__menu-btn');
      const drawer  = $('.hs-nav__drawer');
      if (menuBtn && drawer) {
        on(menuBtn, 'click', () => {
          const open = drawer.classList.toggle('open');
          menuBtn.classList.toggle('open', open);
          document.body.style.overflow = open ? 'hidden' : '';
          menuBtn.setAttribute('aria-expanded', open);
        });

        // Close drawer on link click
        $$('.hs-nav__drawer-link', drawer).forEach(link => {
          on(link, 'click', () => {
            drawer.classList.remove('open');
            menuBtn.classList.remove('open');
            document.body.style.overflow = '';
          });
        });
      }

      // Highlight active nav link
      const path = window.location.pathname;
      $$('.hs-nav__link').forEach(link => {
        const href = link.getAttribute('href') || '';
        if (href && href !== '/' && path.startsWith(href)) {
          link.classList.add('active');
        } else if (href === '/' && path === '/') {
          link.classList.add('active');
        }
      });

      // Dropdown hover (desktop) + click (mobile)
      $$('.hs-dropdown').forEach(dd => {
        const toggle = dd.querySelector('[data-dd-toggle]');
        if (toggle) {
          on(toggle, 'click', (e) => {
            e.stopPropagation();
            dd.classList.toggle('open');
          });
        }
      });

      on(document, 'click', () => {
        $$('.hs-dropdown.open').forEach(dd => dd.classList.remove('open'));
      });
    }

    return { init };
  })();

  /* =========================================================================
     5. SCROLL REVEAL
     ========================================================================= */

  const ScrollReveal = (() => {
    let observer;

    function init() {
      if (!('IntersectionObserver' in window)) {
        // Fallback: reveal everything immediately
        $$('[data-reveal]').forEach(el => el.classList.add('revealed'));
        return;
      }

      observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('revealed');
            observer.unobserve(entry.target);
          }
        });
      }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });

      $$('[data-reveal]').forEach(el => observer.observe(el));
    }

    // Re-scan after dynamic content is added
    function observe(container = document) {
      $$('[data-reveal]:not(.revealed)', container).forEach(el => {
        if (observer) observer.observe(el);
        else el.classList.add('revealed');
      });
    }

    return { init, observe };
  })();

  /* =========================================================================
     6. COUNT-UP ANIMATION
     ========================================================================= */

  const CountUp = (() => {
    function animate(el) {
      const target = parseFloat(el.dataset.counter.replace(/,/g, ''));
      if (isNaN(target)) return;

      const duration  = parseInt(el.dataset.counterDuration || 1800);
      const separator = el.dataset.counterSep || ',';
      const suffix    = el.dataset.counterSuffix || '';
      const decimals  = (el.dataset.counter.includes('.'))
        ? el.dataset.counter.split('.')[1].length : 0;

      let startTime = null;
      const startVal = 0;

      function step(ts) {
        if (!startTime) startTime = ts;
        const progress = Math.min((ts - startTime) / duration, 1);
        // Ease out cubic
        const ease = 1 - Math.pow(1 - progress, 3);
        const current = startVal + (target - startVal) * ease;
        el.textContent = current.toFixed(decimals).replace(/\B(?=(\d{3})+(?!\d))/g, separator) + suffix;
        if (progress < 1) requestAnimationFrame(step);
        else el.textContent = target.toFixed(decimals).replace(/\B(?=(\d{3})+(?!\d))/g, separator) + suffix;
      }

      requestAnimationFrame(step);
    }

    function init() {
      if (!('IntersectionObserver' in window)) {
        $$('[data-counter]').forEach(animate);
        return;
      }

      const obs = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            animate(entry.target);
            obs.unobserve(entry.target);
          }
        });
      }, { threshold: 0.4 });

      $$('[data-counter]').forEach(el => obs.observe(el));
    }

    return { init, animate };
  })();

  /* =========================================================================
     7. RIPPLE EFFECT
     ========================================================================= */

  const Ripple = (() => {
    function attach(el) {
      on(el, 'click', (e) => {
        const rect = el.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height) * 2;
        const x = e.clientX - rect.left - size / 2;
        const y = e.clientY - rect.top  - size / 2;

        const ripple = document.createElement('span');
        ripple.className = 'ripple-effect';
        ripple.style.cssText = `width:${size}px;height:${size}px;left:${x}px;top:${y}px`;
        el.appendChild(ripple);
        setTimeout(() => ripple.remove(), 700);
      });
    }

    function init() {
      $$('.btn-primary, .btn-accent, [data-ripple]').forEach(attach);
    }

    return { init, attach };
  })();

  /* =========================================================================
     8. FORM ENHANCEMENTS
     ========================================================================= */

  const Forms = (() => {
    function init() {
      // Real-time validation feedback
      $$('input[required], select[required], textarea[required]').forEach(field => {
        on(field, 'blur', () => validateField(field));
        on(field, 'input', debounce(() => {
          if (field.classList.contains('touched')) validateField(field);
        }, 400));
      });

      // Mark as touched on first blur
      $$('.form-control').forEach(field => {
        on(field, 'blur', () => field.classList.add('touched'));
      });

      // Loading state on form submit
      $$('form[data-loading]').forEach(form => {
        on(form, 'submit', () => {
          const btn = form.querySelector('[type="submit"]');
          if (btn) {
            btn.classList.add('loading');
            btn.disabled = true;
          }
          if (form.dataset.loading === 'bar') LoadingBar.start();
        });
      });

      // Character counter
      $$('[data-maxlength]').forEach(field => {
        const max     = parseInt(field.dataset.maxlength);
        const countEl = document.createElement('div');
        countEl.className = 'form-hint character-count';
        countEl.textContent = `۰ / ${max}`;
        field.parentNode.appendChild(countEl);

        const update = () => {
          const len = field.value.length;
          countEl.textContent = `${len} / ${max}`;
          countEl.style.color = len > max * 0.9
            ? 'var(--error-500)'
            : len > max * 0.75
              ? 'var(--warning-600)'
              : 'var(--color-text-tertiary)';
        };

        on(field, 'input', update);
        update();
      });

      // Confirm-on-submit for dangerous forms
      $$('form[data-confirm]').forEach(form => {
        on(form, 'submit', async (e) => {
          e.preventDefault();
          const ok = await Modal.confirm({
            title:       form.dataset.confirmTitle   || 'آیا مطمئن هستید؟',
            message:     form.dataset.confirm,
            confirmText: form.dataset.confirmOk      || 'تأیید',
            danger:      form.dataset.confirmDanger  === 'true',
          });
          if (ok) { form.removeEventListener('submit', arguments.callee); form.submit(); }
        });
      });
    }

    function validateField(field) {
      const isValid = field.checkValidity();
      field.classList.toggle('form-control--error',   !isValid);
      field.classList.toggle('form-control--success',  isValid && field.value.length > 0);

      let errEl = field.parentNode.querySelector('.form-error-live');
      if (!isValid) {
        if (!errEl) {
          errEl = document.createElement('div');
          errEl.className = 'form-error form-error-live';
          field.parentNode.appendChild(errEl);
        }
        errEl.textContent = field.validationMessage;
      } else {
        errEl?.remove();
      }
    }

    return { init, validateField };
  })();

  /* =========================================================================
     9. FILTER PANEL (mobile toggle)
     ========================================================================= */

  const Filters = (() => {
    function init() {
      // Mobile filter toggle button
      $$('[data-filter-toggle]').forEach(btn => {
        const target = document.getElementById(btn.dataset.filterToggle);
        if (!target) return;

        on(btn, 'click', () => {
          const open = target.classList.toggle('filter-panel--open');
          btn.setAttribute('aria-expanded', open);
          btn.textContent = open ? 'بستن فیلترها ✕' : 'فیلترها ⚙';
        });
      });

      // Auto-submit on select change inside filter forms
      $$('form.filter-form select, form.filter-form input[type="radio"]').forEach(input => {
        on(input, 'change', () => {
          const form = input.closest('form');
          if (form) {
            LoadingBar.start();
            form.submit();
          }
        });
      });

      // Live search debounce
      $$('input[data-live-search]').forEach(input => {
        on(input, 'input', debounce(() => {
          const form = input.closest('form');
          if (form) { LoadingBar.start(); form.submit(); }
        }, 600));
      });
    }

    return { init };
  })();

  /* =========================================================================
     10. INTERACTIVE COMPONENTS
     ========================================================================= */

  const Components = (() => {
    // Tabs
    function initTabs() {
      $$('[data-tabs]').forEach(container => {
        const tabs    = $$('[data-tab]',        container);
        const panels  = $$('[data-tab-panel]',  container);

        tabs.forEach(tab => {
          on(tab, 'click', () => {
            const target = tab.dataset.tab;
            tabs.forEach(t => {
              t.classList.toggle('active', t.dataset.tab === target);
              t.setAttribute('aria-selected', t.dataset.tab === target);
            });
            panels.forEach(p => {
              const show = p.dataset.tabPanel === target;
              p.hidden = !show;
              if (show) p.classList.add('animate-fade-in');
            });
          });
        });
      });
    }

    // Accordion
    function initAccordions() {
      $$('[data-accordion]').forEach(container => {
        $$('[data-accordion-trigger]', container).forEach(trigger => {
          on(trigger, 'click', () => {
            const item    = trigger.closest('[data-accordion-item]');
            const content = item?.querySelector('[data-accordion-content]');
            if (!content) return;

            const open = item.classList.toggle('open');
            trigger.setAttribute('aria-expanded', open);
            content.style.maxHeight = open ? content.scrollHeight + 'px' : '0';
          });
        });
      });
    }

    // Sticky sidebar — tracks scroll and adjusts top offset
    function initStickySidebar() {
      const sidebars = $$('[data-sticky-sidebar]');
      if (!sidebars.length) return;

      const navHeight = 80;
      sidebars.forEach(sidebar => {
        sidebar.style.position = 'sticky';
        sidebar.style.top = (navHeight + 16) + 'px';
      });
    }

    // Auto-close alerts after delay
    function initAutoClose() {
      $$('[data-auto-close]').forEach(el => {
        const delay = parseInt(el.dataset.autoClose) || 4000;
        setTimeout(() => {
          el.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
          el.style.opacity = '0';
          el.style.transform = 'translateY(-8px)';
          setTimeout(() => el.remove(), 400);
        }, delay);
      });
    }

    // Tooltip (simple, data-tooltip attribute)
    function initTooltips() {
      $$('[data-tooltip]').forEach(el => {
        let tip;

        on(el, 'mouseenter', () => {
          tip = document.createElement('div');
          tip.className = 'hs-tooltip';
          tip.textContent = el.dataset.tooltip;
          tip.style.cssText = `
            position:fixed;
            background:var(--gray-900);
            color:white;
            font-size:12px;
            padding:5px 10px;
            border-radius:6px;
            pointer-events:none;
            z-index:var(--z-tooltip);
            white-space:nowrap;
            opacity:0;
            transition:opacity 0.15s;
          `;
          document.body.appendChild(tip);

          const r = el.getBoundingClientRect();
          tip.style.top  = (r.top - tip.offsetHeight - 8) + 'px';
          tip.style.left = (r.left + r.width / 2 - tip.offsetWidth / 2) + 'px';
          requestAnimationFrame(() => tip.style.opacity = '1');
        });

        on(el, 'mouseleave', () => tip?.remove());
      });
    }

    // Progress bars — animate width from data-progress attribute
    function initProgressBars() {
      const obs = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (!entry.isIntersecting) return;
          const fill = entry.target.querySelector('.progress-fill');
          const pct  = entry.target.dataset.progress || 0;
          if (fill) {
            setTimeout(() => { fill.style.width = pct + '%'; }, 100);
          }
          obs.unobserve(entry.target);
        });
      }, { threshold: 0.3 });

      $$('[data-progress]').forEach(bar => {
        const fill = bar.querySelector('.progress-fill');
        if (fill) fill.style.width = '0%';
        obs.observe(bar);
      });
    }

    // Image lazy loading with fade-in
    function initLazyImages() {
      if (!('IntersectionObserver' in window)) return;

      const obs = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (!entry.isIntersecting) return;
          const img = entry.target;
          const src = img.dataset.src;
          if (src) {
            img.src = src;
            img.style.opacity = '0';
            img.style.transition = 'opacity 0.4s ease';
            img.onload = () => { img.style.opacity = '1'; };
            img.removeAttribute('data-src');
          }
          obs.unobserve(img);
        });
      }, { rootMargin: '200px' });

      $$('img[data-src]').forEach(img => obs.observe(img));
    }

    function init() {
      initTabs();
      initAccordions();
      initStickySidebar();
      initAutoClose();
      initTooltips();
      initProgressBars();
      initLazyImages();
    }

    return { init };
  })();

  /* =========================================================================
     11. DATA BRIDGE — Expose Toast & Modal to Razor views
     ========================================================================= */

  window.HS = {
    Toast,
    Modal,
    LoadingBar,
    // Shorthand helpers
    toast:   (msg, type, opts) => Toast.show(msg, type, opts),
    confirm: (opts)            => Modal.confirm(opts),
  };

  /* =========================================================================
     12. INIT ON DOM READY
     ========================================================================= */

  function init() {
    LoadingBar.init();
    Nav.init();
    ScrollReveal.init();
    CountUp.init();
    Ripple.init();
    Forms.init();
    Filters.init();
    Components.init();
    Toast.processTempData();

    // Wire up [data-modal-open] and [data-modal-close] attributes
    on(document, 'click', (e) => {
      const opener = e.target.closest('[data-modal-open]');
      if (opener) { e.preventDefault(); Modal.open(opener.dataset.modalOpen); }

      const closer = e.target.closest('[data-modal-close]');
      if (closer) { e.preventDefault(); Modal.closeAll(); }
    });

    // Loading bar on navigation
    on(document, 'click', (e) => {
      const link = e.target.closest('a[href]');
      if (!link) return;
      const href = link.getAttribute('href');
      const isExternal = link.hostname && link.hostname !== location.hostname;
      const isHash     = href && href.startsWith('#');
      const isJs       = href && href.startsWith('javascript');
      const hasTarget  = link.target === '_blank';
      if (!isExternal && !isHash && !isJs && !hasTarget) {
        LoadingBar.start();
      }
    });

    on(window, 'pageshow', () => LoadingBar.done());

    console.info('🚀 HomeServices Premium UI — Phase 3 Active');
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

})();
