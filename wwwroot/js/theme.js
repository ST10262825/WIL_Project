// theme.js - Simplified version that works with ApiService
console.log('Theme.js loaded - using ApiService for API calls');

// Apply theme to page
window.applyTheme = function (theme) {
    console.log('Applying theme:', theme);
    document.documentElement.setAttribute('data-theme', theme);
};

// Update button appearance
function updateThemeButton(theme) {
    const icon = document.getElementById('themeIcon');
    const text = document.getElementById('themeText');

    if (icon && text) {
        if (theme === 'dark') {
            icon.className = 'fas fa-sun';
            text.textContent = 'Light Mode';
        } else {
            icon.className = 'fas fa-moon';
            text.textContent = 'Dark Mode';
        }
    }
}

// Expose functions to window for global access
window.updateThemeButton = updateThemeButton;