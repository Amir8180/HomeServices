// ============================================================================
// HomeServices - Enhanced Interactions & Micro-animations
// ============================================================================

(function() {
    'use strict';

    // ========== Utility Functions ==========
    
    const debounce = (func, wait) => {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    };

    // ========== Smooth Scroll for Anchor Links ==========
    
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href === '#' || href === '') return;
            
            const target = document.querySelector(href);
            if (target) {
                e.preventDefault();
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // ========== Enhanced Header on Scroll ==========
    
    const header = document.querySelector('.site-header');
    if (header) {
        let lastScroll = 0;
        const headerHeight = header.offsetHeight;
        
        const handleScroll = () => {
            const currentScroll = window.pageYOffset;
            
            if (currentScroll > headerHeight) {
                header.style.boxShadow = '0 4px 20px rgba(44, 36, 22, 0.12)';
            } else {
                header.style.boxShadow = '';
            }
            
            lastScroll = currentScroll;
        };
        
        window.addEventListener('scroll', debounce(handleScroll, 10));
    }

    // ========== Form Input Animations ==========
    
    const formInputs = document.querySelectorAll('.form-control, .form-select');
    formInputs.forEach(input => {
        // Add focus class to parent for enhanced styling
        input.addEventListener('focus', function() {
            this.parentElement.classList.add('input-focused');
        });
        
        input.addEventListener('blur', function() {
            this.parentElement.classList.remove('input-focused');
        });
        
        // Floating label effect (if label exists)
        const updateLabelState = (input) => {
            const label = input.previousElementSibling;
            if (label && label.classList.contains('form-label')) {
                if (input.value.length > 0 || input === document.activeElement) {
                    label.classList.add('label-floating');
                } else {
                    label.classList.remove('label-floating');
                }
            }
        };
        
        input.addEventListener('input', function() {
            updateLabelState(this);
        });
        
        input.addEventListener('focus', function() {
            updateLabelState(this);
        });
        
        input.addEventListener('blur', function() {
            updateLabelState(this);
        });
        
        // Initial state
        updateLabelState(input);
    });

    // ========== Number Counter Animation ==========
    
    const animateCounter = (element, start, end, duration) => {
        const range = end - start;
        const increment = range / (duration / 16);
        let current = start;
        
        const timer = setInterval(() => {
            current += increment;
            if (current >= end) {
                current = end;
                clearInterval(timer);
            }
            element.textContent = Math.floor(current).toLocaleString('fa-IR');
        }, 16);
    };

    // Observe stat cards and animate when in view
    const statValues = document.querySelectorAll('.stat-value');
    if (statValues.length > 0 && 'IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !entry.target.classList.contains('animated')) {
                    const finalValue = parseInt(entry.target.textContent.replace(/,/g, ''));
                    entry.target.textContent = '0';
                    animateCounter(entry.target, 0, finalValue, 1500);
                    entry.target.classList.add('animated');
                }
            });
        }, { threshold: 0.5 });

        statValues.forEach(stat => observer.observe(stat));
    }

    // ========== Card Hover Parallax Effect ==========
    
    const cards = document.querySelectorAll('.card, .card-service, .card-pro, .category-tile');
    cards.forEach(card => {
        card.addEventListener('mousemove', function(e) {
            if (window.matchMedia('(hover: hover)').matches) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                
                const centerX = rect.width / 2;
                const centerY = rect.height / 2;
                
                const rotateX = (y - centerY) / 30;
                const rotateY = (centerX - x) / 30;
                
                this.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) translateY(-6px)`;
            }
        });
        
        card.addEventListener('mouseleave', function() {
            this.style.transform = '';
        });
    });

    // ========== Toast Notification System ==========
    
    window.showToast = function(message, type = 'info', duration = 3000) {
        const toastContainer = document.getElementById('toast-container') || createToastContainer();
        
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <span class="toast-icon">${getToastIcon(type)}</span>
                <span class="toast-message">${message}</span>
            </div>
            <button class="toast-close" onclick="this.parentElement.remove()">×</button>
        `;
        
        toastContainer.appendChild(toast);
        
        // Trigger animation
        setTimeout(() => toast.classList.add('show'), 10);
        
        // Auto remove
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, duration);
    };
    
    function createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = `
            position: fixed;
            top: var(--space-6, 24px);
            left: var(--space-6, 24px);
            z-index: 10000;
            display: flex;
            flex-direction: column;
            gap: var(--space-3, 12px);
            pointer-events: none;
        `;
        document.body.appendChild(container);
        return container;
    }
    
    function getToastIcon(type) {
        const icons = {
            success: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="width:18px;height:18px;display:block;"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
            error:   '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="width:18px;height:18px;display:block;"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>',
            warning: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="width:18px;height:18px;display:block;"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
            info:    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="width:18px;height:18px;display:block;"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>',
        };
        return icons[type] || icons.info;
    }

    // ========== Lazy Loading Images ==========
    
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (img.dataset.src) {
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                        imageObserver.unobserve(img);
                    }
                }
            });
        });

        document.querySelectorAll('img[data-src]').forEach(img => {
            imageObserver.observe(img);
        });
    }

    // ========== Enhanced Dropdown Animations ==========
    
    const dropdowns = document.querySelectorAll('.dropdown');
    dropdowns.forEach(dropdown => {
        const toggle = dropdown.querySelector('[data-bs-toggle="dropdown"]');
        const menu = dropdown.querySelector('.dropdown-menu');
        
        if (toggle && menu) {
            dropdown.addEventListener('show.bs.dropdown', function() {
                menu.style.display = 'block';
                menu.style.opacity = '0';
                menu.style.transform = 'translateY(-8px) scale(0.96)';
                
                requestAnimationFrame(() => {
                    menu.style.transition = 'all 0.3s cubic-bezier(0.16, 1, 0.3, 1)';
                    menu.style.opacity = '1';
                    menu.style.transform = 'translateY(0) scale(1)';
                });
            });
        }
    });

    // ========== Button Ripple Effect ==========
    
    const buttons = document.querySelectorAll('.btn:not(.btn-link)');
    buttons.forEach(button => {
        button.addEventListener('click', function(e) {
            const ripple = document.createElement('span');
            ripple.className = 'btn-ripple';
            
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;
            
            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                top: ${y}px;
                left: ${x}px;
                background: rgba(255, 255, 255, 0.5);
                border-radius: 50%;
                transform: scale(0);
                animation: ripple 0.6s ease-out;
                pointer-events: none;
            `;
            
            this.style.position = 'relative';
            this.style.overflow = 'hidden';
            this.appendChild(ripple);
            
            setTimeout(() => ripple.remove(), 600);
        });
    });

    // Add ripple animation to stylesheet dynamically
    if (!document.getElementById('ripple-animation')) {
        const style = document.createElement('style');
        style.id = 'ripple-animation';
        style.textContent = `
            @keyframes ripple {
                to {
                    transform: scale(2);
                    opacity: 0;
                }
            }
            .toast {
                background: var(--color-surface-elevated, white);
                padding: var(--space-4, 16px) var(--space-5, 20px);
                border-radius: var(--radius-lg, 14px);
                box-shadow: var(--shadow-lg, 0 8px 32px rgba(0,0,0,0.12));
                display: flex;
                align-items: center;
                gap: var(--space-3, 12px);
                pointer-events: all;
                opacity: 0;
                transform: translateX(100%);
                transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
                border-inline-start: 4px solid;
            }
            .toast.show {
                opacity: 1;
                transform: translateX(0);
            }
            .toast-success { border-color: var(--color-success, #7C9473); }
            .toast-error { border-color: var(--color-error, #C77B5E); }
            .toast-warning { border-color: var(--color-warning, #D4AF6A); }
            .toast-info { border-color: var(--color-info, #8B9EB5); }
            .toast-close {
                background: none;
                border: none;
                font-size: 24px;
                cursor: pointer;
                opacity: 0.5;
                transition: opacity 0.2s;
            }
            .toast-close:hover { opacity: 1; }
        `;
        document.head.appendChild(style);
    }

    // ========== Global Loading Overlay ==========
    
    window.showLoading = function(message = 'در حال بارگذاری...') {
        let overlay = document.getElementById('global-loading-overlay');
        
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'global-loading-overlay';
            overlay.className = 'loading-overlay';
            overlay.innerHTML = `
                <div class="loading-overlay-content">
                    <div class="loading-overlay-spinner"></div>
                    <div class="loading-overlay-text">${message}</div>
                </div>
            `;
            document.body.appendChild(overlay);
        }
        
        overlay.querySelector('.loading-overlay-text').textContent = message;
        
        // Trigger reflow for animation
        overlay.offsetHeight;
        overlay.classList.add('active');
    };
    
    window.hideLoading = function() {
        const overlay = document.getElementById('global-loading-overlay');
        if (overlay) {
            overlay.classList.remove('active');
        }
    };
    
    // ========== Button Loading States ==========
    
    window.setButtonLoading = function(button, loading = true) {
        if (loading) {
            button.classList.add('loading');
            button.disabled = true;
            button.dataset.originalText = button.innerHTML;
        } else {
            button.classList.remove('loading');
            button.disabled = false;
            if (button.dataset.originalText) {
                button.innerHTML = button.dataset.originalText;
                delete button.dataset.originalText;
            }
        }
    };
    
    // Auto-handle form submissions with loading states
    document.querySelectorAll('form[data-loading]').forEach(form => {
        form.addEventListener('submit', function(e) {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                setButtonLoading(submitBtn, true);
            }
            
            // Show global loading if specified
            if (this.dataset.loading === 'overlay') {
                const message = this.dataset.loadingMessage || 'در حال ارسال...';
                showLoading(message);
            }
        });
    });
    
    // ========== Skeleton Screens ==========
    
    window.createSkeletonCard = function() {
        const card = document.createElement('div');
        card.className = 'skeleton-card';
        card.innerHTML = `
            <div class="skeleton-card-image skeleton"></div>
            <div class="skeleton-card-body">
                <div class="skeleton skeleton-title"></div>
                <div class="skeleton skeleton-paragraph"></div>
                <div class="skeleton skeleton-paragraph"></div>
                <div class="skeleton skeleton-paragraph" style="width: 80%;"></div>
            </div>
            <div class="d-flex justify-content-between align-items-center mt-4">
                <div class="skeleton skeleton-badge"></div>
                <div class="skeleton skeleton-button"></div>
            </div>
        `;
        return card;
    };
    
    window.createSkeletonListItem = function() {
        const item = document.createElement('div');
        item.className = 'skeleton-list-item';
        item.innerHTML = `
            <div class="skeleton skeleton-avatar"></div>
            <div class="skeleton-list-item-content">
                <div class="skeleton skeleton-text" style="width: 70%;"></div>
                <div class="skeleton skeleton-text skeleton-text-sm" style="width: 90%;"></div>
                <div class="skeleton skeleton-text skeleton-text-sm" style="width: 50%;"></div>
            </div>
            <div class="skeleton skeleton-button"></div>
        `;
        return item;
    };
    
    window.showSkeletonScreen = function(container, type = 'card', count = 3) {
        if (typeof container === 'string') {
            container = document.querySelector(container);
        }
        
        if (!container) return;
        
        container.innerHTML = '';
        container.classList.add('skeleton-container');
        
        const createFunc = type === 'list' ? createSkeletonListItem : createSkeletonCard;
        
        for (let i = 0; i < count; i++) {
            const skeleton = createFunc();
            skeleton.style.animationDelay = `${i * 100}ms`;
            container.appendChild(skeleton);
        }
    };
    
    window.hideSkeletonScreen = function(container) {
        if (typeof container === 'string') {
            container = document.querySelector(container);
        }
        
        if (!container) return;
        
        container.classList.remove('skeleton-container');
    };
    
    // ========== Progress Bar Helper ==========
    
    window.updateProgressBar = function(element, percentage) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return;
        
        const fill = element.querySelector('.progress-bar-fill');
        if (fill) {
            fill.style.width = `${Math.min(100, Math.max(0, percentage))}%`;
        }
    };
    
    window.createProgressBar = function(container, indeterminate = false) {
        if (typeof container === 'string') {
            container = document.querySelector(container);
        }
        
        if (!container) return null;
        
        const progressBar = document.createElement('div');
        progressBar.className = 'progress-bar' + (indeterminate ? ' progress-bar-indeterminate' : '');
        progressBar.innerHTML = '<div class="progress-bar-fill" style="width: 0%;"></div>';
        
        container.appendChild(progressBar);
        return progressBar;
    };
    
    // ========== Async Content Loader ==========
    
    window.loadContent = async function(url, container, options = {}) {
        const {
            method = 'GET',
            data = null,
            showSkeleton = true,
            skeletonType = 'card',
            skeletonCount = 3,
            onSuccess = null,
            onError = null
        } = options;
        
        if (typeof container === 'string') {
            container = document.querySelector(container);
        }
        
        if (!container) return;
        
        try {
            // Show skeleton screen
            if (showSkeleton) {
                showSkeletonScreen(container, skeletonType, skeletonCount);
            }
            
            // Fetch content
            const fetchOptions = {
                method,
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            };
            
            if (data && method !== 'GET') {
                fetchOptions.body = JSON.stringify(data);
            }
            
            const response = await fetch(url, fetchOptions);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const html = await response.text();
            
            // Fade out skeleton
            if (showSkeleton) {
                container.style.opacity = '0';
                await new Promise(resolve => setTimeout(resolve, 200));
            }
            
            // Update content
            container.innerHTML = html;
            
            // Fade in new content
            container.style.opacity = '0';
            container.offsetHeight; // Trigger reflow
            container.style.transition = 'opacity 0.3s ease-out';
            container.style.opacity = '1';
            
            // Trigger stagger animations if present
            const staggerContainer = container.querySelector('[data-stagger]');
            if (staggerContainer) {
                staggerContainer.offsetHeight; // Trigger animation
            }
            
            if (onSuccess) {
                onSuccess(html);
            }
            
            hideSkeletonScreen(container);
            
        } catch (error) {
            console.error('Content loading error:', error);
            
            container.innerHTML = `
                <div class="alert alert-danger">
                    <strong>خطا در بارگذاری</strong><br>
                    لطفاً دوباره تلاش کنید.
                </div>
            `;
            
            if (onError) {
                onError(error);
            }
        }
    };
    
    // ========== Lazy Load Animations on Scroll ==========
    
    if ('IntersectionObserver' in window) {
        const animateObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('fade-in-up');
                    animateObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1, rootMargin: '50px' });
        
        // Auto-observe elements with data-animate attribute
        document.querySelectorAll('[data-animate]').forEach(element => {
            animateObserver.observe(element);
        });
    }
    
    // ========== Initialize on Page Load ==========
    
    console.log('HomeServices UI Enhanced - Premium Edition Loaded');
    console.log('Loading States & Skeleton Screens Active');

})();
