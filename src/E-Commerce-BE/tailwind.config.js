module.exports = {
    content: [
        './Views/**/*.cshtml',
        './Pages/**/*.cshtml',
        './wwwroot/js/**/*.js'
    ],
    theme: {
        extend: {
            colors: {
                'brand-amber': '#f59e0b',
                'brand-gray': '#1f2937',
                'brand-dark': '#0f172a'
            },
            keyframes: {
                slideInLeft: {
                    'from': { opacity: '0', transform: 'translateX(-50px)' },
                    'to': { opacity: '1', transform: 'translateX(0)' },
                },
                slideInRight: {
                    'from': { opacity: '0', transform: 'translateX(50px)' },
                    'to': { opacity: '1', transform: 'translateX(0)' },
                },
                fadeInUp: {
                    'from': { opacity: '0', transform: 'translateY(30px)' },
                    'to': { opacity: '1', transform: 'translateY(0)' },
                },
                'bounce-gentle': {
                    '0%, 100%': { transform: 'translateY(0)' },
                    '50%': { transform: 'translateY(-6px)' },
                },
                'pulse-glow-amber': {
                    '0%, 100%': { boxShadow: '0 0 15px 0px rgba(245, 158, 11, 0.3), 0 0 5px 0px rgba(245, 158, 11, 0.2)' },
                    '50%': { boxShadow: '0 0 30px 5px rgba(245, 158, 11, 0.5), 0 0 10px 2px rgba(245, 158, 11, 0.4)' },
                },
                'float-slow': {
                    '0%, 100%': { transform: 'translateY(0px) rotate(0deg)' },
                    '33%': { transform: 'translateY(-15px) rotate(120deg)' },
                    '66%': { transform: 'translateY(-8px) rotate(240deg)' },
                },
                'shimmer': {
                    '0%, 100%': { backgroundPosition: '200% 0' },
                    '50%': { backgroundPosition: '-200% 0' },
                },
                'glow-pulse': {
                    '0%, 100%': { 
                        opacity: '0.5', 
                        transform: 'scale(1)',
                        filter: 'blur(1px)'
                    },
                    '50%': { 
                        opacity: '0.8', 
                        transform: 'scale(1.1)',
                        filter: 'blur(0px)'
                    },
                },
                'slide-up-stagger': {
                    '0%': { 
                        opacity: '0', 
                        transform: 'translateY(40px)',
                        filter: 'blur(4px)'
                    },
                    '100%': { 
                        opacity: '1', 
                        transform: 'translateY(0)',
                        filter: 'blur(0px)'
                    },
                },
                'scale-in': {
                    '0%': { 
                        opacity: '0', 
                        transform: 'scale(0.8) rotate(-5deg)',
                        filter: 'blur(2px)'
                    },
                    '100%': { 
                        opacity: '1', 
                        transform: 'scale(1) rotate(0deg)',
                        filter: 'blur(0px)'
                    },
                }
            },
            animation: {
                'slide-in-left': 'slideInLeft 1s ease-out forwards',
                'slide-in-right': 'slideInRight 1s ease-out forwards',
                'fade-in-up': 'fadeInUp 0.8s ease-out forwards',
                'bounce-gentle': 'bounce-gentle 2.5s infinite ease-in-out',
                'pulse-glow-amber': 'pulse-glow-amber 2.5s infinite ease-in-out',
                'float-slow': 'float-slow 8s infinite ease-in-out',
                'shimmer': 'shimmer 3s ease-in-out infinite',
                'glow-pulse': 'glow-pulse 4s infinite ease-in-out',
                'slide-up-stagger': 'slide-up-stagger 0.8s ease-out forwards',
                'scale-in': 'scale-in 1s ease-out forwards',
            },
            transitionTimingFunction: {
                'bounce-in': 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
                'smooth': 'cubic-bezier(0.4, 0, 0.2, 1)',
                'elastic': 'cubic-bezier(0.175, 0.885, 0.32, 1.275)',
            }
        },
    },
    plugins: [],
}