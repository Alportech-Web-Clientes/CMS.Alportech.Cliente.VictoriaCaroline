document.addEventListener("DOMContentLoaded", () => {
    // DOM Elements
    const switchFormLinks = document.querySelectorAll(".switch-form")
    const formPanels = document.querySelectorAll(".form-panel")
    const forgotPasswordLink = document.querySelector(".forgot-password")

    // Setup Event Listeners
    function setupEventListeners() {
        // Switch between forms
        switchFormLinks.forEach((link) => {
            link.addEventListener("click", function (e) {
                e.preventDefault()
                const targetForm = this.getAttribute("data-target")
                switchForm(targetForm)
            })
        })

        // Forgot password link
        forgotPasswordLink.addEventListener("click", (e) => {
            e.preventDefault()
            switchForm("forgot")
        })

        // Botões de mostrar/ocultar senha
        document.querySelectorAll(".password-toggle").forEach((button) => {
            button.addEventListener("click", togglePasswordVisibility)
        })
    }

    // Switch between form panels
    function switchForm(targetForm) {
        formPanels.forEach((panel) => {
            panel.classList.remove("active")
        })

        const targetPanel = document.querySelector(`.${targetForm}-panel`)
        if (targetPanel) {
            targetPanel.classList.add("active")
        }
    }

    // Função para mostrar/ocultar senha
    function togglePasswordVisibility() {
        const passwordField = this.parentElement.querySelector("input")

        if (passwordField.type === "password") {
            passwordField.type = "text"
            this.classList.add("active")
            this.setAttribute("aria-label", "Ocultar senha")
            this.innerHTML = '<span class="eye-icon">👁️‍🗨️</span>' // Olho aberto
        } else {
            passwordField.type = "password"
            this.classList.remove("active")
            this.setAttribute("aria-label", "Mostrar senha")
            this.innerHTML = '<span class="eye-icon">👁️</span>' // Olho fechado
        }
    }

    // Initialize
    setupEventListeners();
})