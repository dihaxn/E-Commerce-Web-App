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
                }
            },
            animation: {
                'slide-in-left': 'slideInLeft 1s ease-out forwards',
                'slide-in-right': 'slideInRight 1s ease-out forwards',
                'fade-in-up': 'fadeInUp 0.8s ease-out forwards',
                'bounce-gentle': 'bounce-gentle 2.5s infinite ease-in-out',
                'pulse-glow-amber': 'pulse-glow-amber 2.5s infinite ease-in-out',
            }
        },
    },
    plugins: [],
}