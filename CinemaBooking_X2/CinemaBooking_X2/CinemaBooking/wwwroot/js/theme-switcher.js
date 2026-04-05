/**
 * Theme Switcher - Chuyển đổi giữa chế độ sáng và tối
 */
document.addEventListener('DOMContentLoaded', function() {
    // Kiểm tra theme đã lưu trong localStorage
    const savedTheme = localStorage.getItem('theme');
    const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');
    
    // Thiết lập theme ban đầu
    if (savedTheme) {
        document.body.classList.toggle('light-theme', savedTheme === 'light');
        document.body.classList.toggle('dark-theme', savedTheme === 'dark');
    } else {
        // Nếu chưa có theme được lưu, sử dụng theme mặc định của hệ thống
        document.body.classList.toggle('dark-theme', prefersDarkScheme.matches);
        document.body.classList.toggle('light-theme', !prefersDarkScheme.matches);
    }
    
    // Cập nhật trạng thái nút chuyển đổi
    updateThemeToggle();
    
    // Xử lý sự kiện khi người dùng nhấn nút chuyển đổi theme
    const themeToggle = document.getElementById('theme-toggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', function() {
            // Kiểm tra theme hiện tại
            if (document.body.classList.contains('light-theme')) {
                document.body.classList.replace('light-theme', 'dark-theme');
                localStorage.setItem('theme', 'dark');
            } else {
                document.body.classList.replace('dark-theme', 'light-theme');
                localStorage.setItem('theme', 'light');
            }
            
            // Cập nhật trạng thái nút chuyển đổi
            updateThemeToggle();
        });
    }
    
    // Hàm cập nhật trạng thái nút chuyển đổi theme
    function updateThemeToggle() {
        const themeToggle = document.getElementById('theme-toggle');
        const themeIcon = document.getElementById('theme-icon');
        
        if (themeToggle && themeIcon) {
            if (document.body.classList.contains('light-theme')) {
                themeIcon.classList.replace('fa-sun', 'fa-moon');
                themeToggle.setAttribute('title', 'Chuyển sang chế độ tối');
                themeToggle.setAttribute('aria-label', 'Chuyển sang chế độ tối');
            } else {
                themeIcon.classList.replace('fa-moon', 'fa-sun');
                themeToggle.setAttribute('title', 'Chuyển sang chế độ sáng');
                themeToggle.setAttribute('aria-label', 'Chuyển sang chế độ sáng');
            }
        }
    }
});
