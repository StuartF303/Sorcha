// Sorcha Landing Page JavaScript
// Optional: Fetch public statistics from the API

(function() {
    'use strict';

    // Try to fetch public stats from the API
    async function loadStats() {
        try {
            const response = await fetch('/api/public/stats', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (response.ok) {
                const stats = await response.json();
                updateStatDisplay('stat-blueprints', stats.blueprints ?? 0);
                updateStatDisplay('stat-transactions', stats.transactions ?? 0);
                updateStatDisplay('stat-peers', stats.activePeers ?? 0);
            } else {
                // If API not available, show placeholder values
                setPlaceholderStats();
            }
        } catch (error) {
            // Silently fail and show placeholder values
            console.log('Stats API not available, using placeholders');
            setPlaceholderStats();
        }
    }

    function updateStatDisplay(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            // Animate the number
            animateNumber(element, 0, value, 1000);
        }
    }

    function animateNumber(element, start, end, duration) {
        const startTime = performance.now();

        function update(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easeProgress = 1 - Math.pow(1 - progress, 3); // Ease out cubic

            const current = Math.round(start + (end - start) * easeProgress);
            element.textContent = formatNumber(current);

            if (progress < 1) {
                requestAnimationFrame(update);
            }
        }

        requestAnimationFrame(update);
    }

    function formatNumber(num) {
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1) + 'M';
        } else if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'K';
        }
        return num.toString();
    }

    function setPlaceholderStats() {
        // Show meaningful placeholder values indicating platform readiness
        updateStatDisplay('stat-blueprints', 3);
        updateStatDisplay('stat-transactions', 0);
        updateStatDisplay('stat-peers', 1);
    }

    // Smooth scroll for anchor links
    function setupSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function(e) {
                e.preventDefault();
                const target = document.querySelector(this.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    // Add scroll-based effects
    function setupScrollEffects() {
        const nav = document.querySelector('.nav');
        let lastScroll = 0;

        window.addEventListener('scroll', () => {
            const currentScroll = window.pageYOffset;

            // Add shadow to nav on scroll
            if (currentScroll > 50) {
                nav.style.boxShadow = '0 2px 10px rgba(0, 0, 0, 0.1)';
            } else {
                nav.style.boxShadow = 'none';
            }

            lastScroll = currentScroll;
        });
    }

    // Initialize
    document.addEventListener('DOMContentLoaded', function() {
        loadStats();
        setupSmoothScroll();
        setupScrollEffects();
    });
})();
