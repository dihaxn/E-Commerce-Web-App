function getShoppingCart() {
    const cookieName = "shopping_cart";
    let cookiesArray = document.cookie.split(';');
    for (let i = 0; i < cookiesArray.length; i++) {
        let cookie = cookiesArray[i].trim();
        if (cookie.startsWith(cookieName + "=")) {
            let cookie_value = cookie.substring(cookieName.length + 1);
            try {
                // Decode from Base64 and parse JSON
                return JSON.parse(atob(cookie_value));
            } catch (exception) {
                // Log error and return empty object if parsing fails
                console.error("Error parsing shopping cart cookie:", exception);
                break;
            }
        }
    }
    return {};
}

function saveCart(cart) {
    try {
        // Encode to Base64
        let cartStr = btoa(JSON.stringify(cart));
        let d = new Date();
        // Set cookie to expire in 1 year
        d.setDate(d.getDate() + 365);
        let expires = d.toUTCString();
        // Use secure, SameSite=Strict cookie attributes for better security
        document.cookie = `shopping_cart=${cartStr};expires=${expires};path=/;SameSite=Strict;Secure`;
    } catch (exception) {
        console.error("Error saving shopping cart:", exception);
    }
}

function addToCart(button, id) {
    let cart = getShoppingCart();
    // Ensure quantity is a number before incrementing
    cart[id] = (Number(cart[id]) || 0) + 1;
    saveCart(cart);

    const buttonText = button.querySelector('.button-text');
    const icon = button.querySelector('i');
    const originalText = buttonText ? buttonText.textContent : 'Add to Cart';
    const originalIconClass = icon ? icon.className : 'bi bi-cart-plus';

    // Disable button and show loading state
    button.disabled = true;
    button.classList.add('opacity-75', 'cursor-not-allowed');
    if (buttonText) buttonText.textContent = 'Adding...';
    if (icon) icon.className = 'bi bi-hourglass-split animate-spin';

    // Simulate an API call or async operation
    setTimeout(() => {
        // Update to success state
        button.classList.remove('btn-primary', 'opacity-75');
        button.classList.add('btn-success');
        if (buttonText) buttonText.textContent = 'Added!';
        if (icon) icon.className = 'bi bi-check-circle';

        // Update the cart size display on the page
        updateCartSize();

        // Revert button to its original state after a delay
        setTimeout(() => {
            button.disabled = false;
            button.classList.remove('btn-success', 'cursor-not-allowed');
            button.classList.add('btn-primary');
            if (buttonText) buttonText.textContent = originalText;
            if (icon) icon.className = originalIconClass;
        }, 2000);

    }, 800);
}

function increase(id) {
    let cart = getShoppingCart();
    cart[id] = (Number(cart[id]) || 0) + 1;
    saveCart(cart);
    // Reload the page to reflect the change
    location.reload();
}

function decrease(id) {
    let cart = getShoppingCart();
    let quantity = Number(cart[id]);
    if (!isNaN(quantity) && quantity > 1) {
        cart[id] = quantity - 1;
        saveCart(cart);
        location.reload();
    }
}

function removeItem(id) {
    let cart = getShoppingCart();
    if (cart[id]) {
        delete cart[id];
        saveCart(cart);
        location.reload();
    }
}

function updateCartSize() {
    const el = document.getElementById("CartSize");
    if (!el) return;

    let cart = getShoppingCart();
    // Sum up all item quantities in the cart
    let cartSize = Object.values(cart).reduce((sum, qty) => sum + (isNaN(qty) ? 0 : Number(qty)), 0);
    
    el.textContent = cartSize || 0;
    
    // Optional: Animate the badge when it updates
    if (cartSize > 0) {
        el.classList.add('animate-bounce');
        setTimeout(() => el.classList.remove('animate-bounce'), 500);
    }
}

// Initialize cart size when the DOM is fully loaded
document.addEventListener('DOMContentLoaded', function () {
    updateCartSize();
});
