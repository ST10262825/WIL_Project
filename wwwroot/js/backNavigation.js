// Enhanced back navigation functionality
class BackNavigation {
    constructor() {
        this.history = JSON.parse(sessionStorage.getItem('pageHistory') || '[]');
        this.init();
    }

    init() {
        // Store current page
        this.storeCurrentPage();

        // Add event listeners
        this.addEventListeners();
    }

    storeCurrentPage() {
        const currentPage = window.location.href;

        // Only add if it's different from the last page
        if (this.history.length === 0 || this.history[this.history.length - 1] !== currentPage) {
            this.history.push(currentPage);

            // Keep only last 10 pages in history
            if (this.history.length > 10) {
                this.history.shift();
            }

            sessionStorage.setItem('pageHistory', JSON.stringify(this.history));
        }
    }

    goBack() {
        // Remove current page from history
        this.history.pop();
        sessionStorage.setItem('pageHistory', JSON.stringify(this.history));

        if (this.history.length > 0) {
            // Go to previous page in history
            const previousPage = this.history[this.history.length - 1];
            window.location.href = previousPage;
        } else if (document.referrer && document.referrer.includes(window.location.host)) {
            // Fallback to browser history
            window.history.back();
        } else {
            // Fallback to home
            window.location.href = '/';
        }
    }

    addEventListeners() {
        // Click event for back buttons
        document.addEventListener('click', (e) => {
            if (e.target.closest('.back-btn') || e.target.closest('[data-action="back"]')) {
                e.preventDefault();
                this.goBack();
            }
        });

        // Keyboard shortcut
        document.addEventListener('keydown', (e) => {
            if (e.altKey && e.key === 'ArrowLeft') {
                e.preventDefault();
                this.goBack();
            }
        });

        // Swipe back on mobile (optional)
        this.addSwipeSupport();
    }

    addSwipeSupport() {
        // Simple swipe detection for mobile
        let touchStartX = 0;

        document.addEventListener('touchstart', (e) => {
            touchStartX = e.changedTouches[0].screenX;
        });

        document.addEventListener('touchend', (e) => {
            const touchEndX = e.changedTouches[0].screenX;
            const diffX = touchEndX - touchStartX;

            // Swipe right to go back
            if (diffX > 100 && touchStartX < 50) { // Start from left edge
                this.goBack();
            }
        });
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.backNavigation = new BackNavigation();
});