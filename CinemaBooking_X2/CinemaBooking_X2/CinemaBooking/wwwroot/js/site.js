// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Hiệu ứng hiển thị thông báo lỗi và thành công
document.addEventListener('DOMContentLoaded', function () {
    setTimeout(function () {
        const alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function (alert) {
            // Tự động ẩn thông báo sau 5 giây
            setTimeout(function () {
                if (alert) {
                    const bsAlert = new bootstrap.Alert(alert);
                    bsAlert.close();
                }
            }, 5000);
        });
    }, 500);
});

// Add navbar scroll effect
window.addEventListener('DOMContentLoaded', () => {
    const navbar = document.querySelector('.navbar');
    
    // Function to handle navbar scrolling effect
    function handleNavbarScroll() {
        if (!navbar) return;
        if (window.scrollY > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    }
    
    // Add scroll event listener
    window.addEventListener('scroll', handleNavbarScroll);
    
    // Initial check for page refresh in middle of page
    handleNavbarScroll();
});

// Trailer autoplay
document.addEventListener('DOMContentLoaded', () => {
    // Handle movie card trailers
    const movieCards = document.querySelectorAll('.movie-card');
    
    movieCards.forEach(card => {
        if (!card) return;
        
        const trailer = card.querySelector('.movie-trailer');
        if (!trailer) return;
        
        card.addEventListener('mouseenter', () => {
            if (trailer.getAttribute('data-src') && !trailer.src) {
                trailer.src = trailer.getAttribute('data-src');
            }
            trailer.muted = true;
            trailer.play().catch(err => console.log('Autoplay prevented:', err));
        });
        
        card.addEventListener('mouseleave', () => {
            trailer.pause();
            trailer.currentTime = 0;
        });
    });
    
    // Close alerts automatically after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const closeBtn = alert.querySelector('.btn-close');
            if (closeBtn) closeBtn.click();
        }, 5000);
    });

    // Search Bar 2026 Toggle Logic
    const searchBar = document.querySelector('.search-bar-modern');
    if (searchBar) {
        const searchInput = searchBar.querySelector('input');
        const searchIcon = searchBar.querySelector('.input-group-text');

        if (searchIcon && searchInput) {
            searchIcon.addEventListener('click', (e) => {
                e.preventDefault();
                searchInput.focus();
            });
        }
    }
});
