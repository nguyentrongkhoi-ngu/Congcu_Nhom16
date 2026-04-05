/**
 * Effects.js - Các hiệu ứng tương tác và hiệu ứng hình ảnh
 */

document.addEventListener('DOMContentLoaded', function() {
    // Hiệu ứng Parallax cho background
    window.addEventListener('scroll', function() {
        const scrollPosition = window.pageYOffset;
        const parallaxElements = document.querySelectorAll('.parallax');

        parallaxElements.forEach(element => {
            const speed = element.getAttribute('data-speed') || 0.5;
            element.style.transform = `translateY(${scrollPosition * speed}px)`;
        });
    });

    // Hiệu ứng hover 3D cho movie cards
    const movieCards = document.querySelectorAll('.movie-card');

    movieCards.forEach(card => {
        card.addEventListener('mousemove', handleHover3D);
        card.addEventListener('mouseleave', resetHover3D);
        card.addEventListener('mouseenter', enterHover3D);
    });

    function enterHover3D() {
        // Thêm class để kích hoạt transition
        this.classList.add('hover-active');

        // Thêm hiệu ứng shadow
        this.style.boxShadow = '0 15px 30px rgba(0, 0, 0, 0.4), 0 5px 15px rgba(0, 0, 0, 0.3)';
    }

    function handleHover3D(e) {
        const card = this;
        const cardRect = card.getBoundingClientRect();
        const cardWidth = cardRect.width;
        const cardHeight = cardRect.height;
        const centerX = cardRect.left + cardWidth / 2;
        const centerY = cardRect.top + cardHeight / 2;
        const mouseX = e.clientX - centerX;
        const mouseY = e.clientY - centerY;

        // Giảm độ nghiêng để hiệu ứng tinh tế hơn
        const rotateX = (mouseY / (cardHeight / 2)) * -7; // Max 7 degrees
        const rotateY = (mouseX / (cardWidth / 2)) * 7; // Max 7 degrees

        // Thêm translateY để tạo hiệu ứng nổi lên
        card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.05, 1.05, 1.05) translateY(-10px)`;

        // Hiệu ứng ánh sáng theo chuột
        const glare = card.querySelector('.card-glare');
        if (glare) {
            const glareX = (mouseX / cardWidth) * 100 + 50;
            const glareY = (mouseY / cardHeight) * 100 + 50;
            glare.style.opacity = '1';
            glare.style.background = `radial-gradient(circle at ${glareX}% ${glareY}%, rgba(255,255,255,0.4) 0%, rgba(255,255,255,0) 60%)`;
        }

        // Hiệu ứng cho các phần tử con
        const cardTitle = card.querySelector('.card-title');
        const cardContent = card.querySelector('.card-content');

        if (cardTitle) {
            cardTitle.style.transform = `translateZ(20px)`;
        }

        if (cardContent) {
            cardContent.style.transform = `translateZ(10px)`;
        }
    }

    function resetHover3D() {
        // Xóa class hover-active
        this.classList.remove('hover-active');

        // Reset transform
        this.style.transform = 'perspective(1000px) rotateX(0) rotateY(0) scale3d(1, 1, 1) translateY(0)';

        // Reset shadow
        this.style.boxShadow = '0 10px 20px rgba(0, 0, 0, 0.2)';

        // Reset hiệu ứng ánh sáng
        const glare = this.querySelector('.card-glare');
        if (glare) {
            glare.style.opacity = '0';
            glare.style.background = 'none';
        }

        // Reset hiệu ứng cho các phần tử con
        const cardTitle = this.querySelector('.card-title');
        const cardContent = this.querySelector('.card-content');

        if (cardTitle) {
            cardTitle.style.transform = 'translateZ(0)';
        }

        if (cardContent) {
            cardContent.style.transform = 'translateZ(0)';
        }
    }

    // Thêm hiệu ứng ánh sáng cho movie cards
    movieCards.forEach(card => {
        // Tạo phần tử ánh sáng
        const glare = document.createElement('div');
        glare.classList.add('card-glare');
        glare.style.position = 'absolute';
        glare.style.top = '0';
        glare.style.left = '0';
        glare.style.width = '100%';
        glare.style.height = '100%';
        glare.style.pointerEvents = 'none';
        glare.style.zIndex = '1';

        // Thêm vào card
        card.style.position = 'relative';
        card.style.overflow = 'hidden';
        card.appendChild(glare);
    });

    // Hiệu ứng Reveal khi scroll
    const revealElements = document.querySelectorAll('.reveal');

    function checkReveal() {
        const windowHeight = window.innerHeight;
        const revealPoint = 150;

        revealElements.forEach(element => {
            const elementTop = element.getBoundingClientRect().top;

            if (elementTop < windowHeight - revealPoint) {
                element.classList.add('active');
            }
        });
    }

    // Kích hoạt ngay khi trang tải xong
    function initialReveal() {
        revealElements.forEach(element => {
            element.classList.add('active');
        });
    }

    // Đảm bảo hiệu ứng reveal hoạt động ngay cả khi không scroll
    setTimeout(initialReveal, 300);

    window.addEventListener('scroll', checkReveal);
    window.addEventListener('load', checkReveal);

    // Hiệu ứng nút nhấp nháy
    const pulseButtons = document.querySelectorAll('.btn-pulse');

    pulseButtons.forEach(button => {
        button.addEventListener('mouseover', function() {
            this.classList.add('pulse-animation');
        });

        button.addEventListener('mouseout', function() {
            this.classList.remove('pulse-animation');
        });
    });

    // Hiệu ứng ripple cho các nút
    const rippleButtons = document.querySelectorAll('.btn-ripple');

    rippleButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            const rect = this.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            const ripple = document.createElement('span');
            ripple.classList.add('ripple-effect');
            ripple.style.left = `${x}px`;
            ripple.style.top = `${y}px`;

            this.appendChild(ripple);

            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    });

    // Thêm class ripple cho tất cả các nút
    document.querySelectorAll('.btn').forEach(btn => {
        btn.classList.add('btn-ripple');
    });

    // Hiệu ứng typing cho hero section
    const heroText = document.querySelector('.hero-typing-text');
    if (heroText) {
        const text = heroText.textContent;
        heroText.textContent = '';

        let i = 0;
        function typeWriter() {
            if (i < text.length) {
                heroText.textContent += text.charAt(i);
                i++;
                setTimeout(typeWriter, 100);
            }
        }

        typeWriter();
    }
});
