document.addEventListener("DOMContentLoaded", () => {
    // DOM Elements
    const navLinks = document.querySelectorAll(".nav-link")
    const pages = document.querySelectorAll(".dashboard-page")
    const themeToggle = document.getElementById("themeToggle")
    const body = document.body
    const actionButtons = document.querySelectorAll(".action-btn")
    const logoutBtn = document.getElementById("logoutBtn")

    // Project Elements
    const addProjectBtn = document.getElementById("addProjectBtn")
    const projectModal = document.getElementById("projectModal")
    const projectForm = document.getElementById("projectForm")
    const projectsTable = document.getElementById("projectsTable").querySelector("tbody")

    // Experience Elements
    const addExperienceBtn = document.getElementById("addExperienceBtn")
    const experienceModal = document.getElementById("experienceModal")
    const experienceForm = document.getElementById("experienceForm")
    const experienceTable = document.getElementById("experienceTable").querySelector("tbody")

    // Education Elements
    const addEducationBtn = document.getElementById("addEducationBtn")
    const educationModal = document.getElementById("educationModal")
    const educationForm = document.getElementById("educationForm")
    const educationTable = document.getElementById("educationTable").querySelector("tbody")

    // Skills Elements
    const addSkillBtn = document.getElementById("addSkillBtn")
    const skillModal = document.getElementById("skillModal")
    const skillForm = document.getElementById("skillForm")
    const skillsTable = document.getElementById("skillsTable").querySelector("tbody")
    const skillLevel = document.getElementById("skillLevel")
    const skillLevelOutput = document.getElementById("skillLevelOutput")

    // Contact Elements
    const contactForm = document.getElementById("contactInfoForm")
    const addSocialBtn = document.getElementById("addSocialBtn")
    const socialLinks = document.getElementById("socialLinks")

    // Settings Elements
    const exportDataBtn = document.getElementById("exportDataBtn")
    const importDataBtn = document.getElementById("importDataBtn")
    const importFile = document.getElementById("importFile")
    const updatePasswordBtn = document.getElementById("updatePasswordBtn")
    const primaryColorInput = document.getElementById("primaryColor")
    const secondaryColorInput = document.getElementById("secondaryColor")
    const defaultThemeSelect = document.getElementById("defaultTheme")

    // Dashboard Counters
    const projectsCount = document.getElementById("projectsCount")
    const experienceCount = document.getElementById("experienceCount")
    const educationCount = document.getElementById("educationCount")
    const skillsCount = document.getElementById("skillsCount")
    const activityList = document.getElementById("activityList")

    // Data Storage
    let projects = JSON.parse(localStorage.getItem("projects")) || []
    let experiences = JSON.parse(localStorage.getItem("experiences")) || []
    let education = JSON.parse(localStorage.getItem("education")) || []
    let skills = JSON.parse(localStorage.getItem("skills")) || []
    let contactInfo = JSON.parse(localStorage.getItem("contactInfo")) || {
        fullName: "Victoria Caroline de Souza Alves",
        title: "Especialista Ambiental",
        email: "victoria.caroline@email.com",
        phone: "+55 (11) 98765-4321",
        location: "São Paulo, SP - Brasil",
        aboutMe:
            "Sou Victoria Caroline de Souza Alves, uma especialista ambiental com foco em desenvolvimento sustentável e gestão de recursos hídricos.",
        socialLinks: [{ type: "linkedin", url: "https://www.linkedin.com/in/victoria-caroline-de-souza-alves-663814186/" }],
    }

    // Initialize the application
    function init() {
        loadTheme()
        setupEventListeners()
        loadData()
        updateCounters()
    }

    // Load saved theme
    function loadTheme() {
        const savedTheme = localStorage.getItem("theme")
        if (savedTheme === "dark") {
            body.classList.add("dark-theme")
            defaultThemeSelect.value = "dark"
        } else {
            defaultThemeSelect.value = "light"
        }
    }

    // Setup Event Listeners
    function setupEventListeners() {
        // Navigation
        navLinks.forEach((link) => {
            link.addEventListener("click", function (e) {
                e.preventDefault()
                const targetId = this.getAttribute("data-page")
                pages.forEach((page) => page.classList.remove("active"))
                document.getElementById(targetId).classList.add("active")
                navLinks.forEach((navLink) => navLink.classList.remove("active"))
                this.classList.add("active")
            })
        })

        // Theme Toggle
        themeToggle.addEventListener("click", toggleTheme)

        // Quick Action Buttons
        actionButtons.forEach((btn) => {
            btn.addEventListener("click", function () {
                const target = this.getAttribute("data-target")
                navLinks.forEach((link) => {
                    if (link.getAttribute("data-page") === target) {
                        link.click()
                    }
                })

                // Trigger add modal
                setTimeout(() => {
                    if (target === "projects") addProjectBtn.click()
                    if (target === "experience") addExperienceBtn.click()
                    if (target === "education") addEducationBtn.click()
                    if (target === "skills") addSkillBtn.click()
                }, 300)
            })
        })

        // Logout Button
        logoutBtn.addEventListener("click", () => {
            alert("Logout functionality would be implemented here.")
        })

        // Projects
        setupProjectListeners()

        // Experience
        setupExperienceListeners()

        // Education
        setupEducationListeners()

        // Skills
        setupSkillListeners()

        // Contact
        setupContactListeners()

        // Settings
        setupSettingsListeners()

        // Close Modals
        document.querySelectorAll(".close-modal, .cancel-btn").forEach((element) => {
            element.addEventListener("click", () => {
                document.querySelectorAll(".modal").forEach((modal) => {
                    modal.style.display = "none"
                })
            })
        })

        // Close modal when clicking outside
        window.addEventListener("click", (e) => {
            document.querySelectorAll(".modal").forEach((modal) => {
                if (e.target === modal) {
                    modal.style.display = "none"
                }
            })
        })
    }

    // Toggle Theme
    function toggleTheme() {
        body.classList.toggle("dark-theme")
        localStorage.setItem("theme", body.classList.contains("dark-theme") ? "dark" : "light")
    }

    // Load Data
    function loadData() {
        renderProjects()
        renderExperiences()
        renderEducation()
        renderSkills()
        loadContactInfo()
    }

    // Update Counters
    function updateCounters() {
        projectsCount.textContent = projects.length
        experienceCount.textContent = experiences.length
        educationCount.textContent = education.length
        skillsCount.textContent = skills.length
    }

    // Add Activity
    function addActivity(text) {
        const now = new Date()
        const timeString = now.toLocaleTimeString()

        const activityItem = document.createElement("div")
        activityItem.className = "activity-item"
        activityItem.innerHTML = `
            <span class="activity-icon">🔄</span>
            <div class="activity-content">
                <p class="activity-text">${text}</p>
                <p class="activity-time">${timeString}</p>
            </div>
        `

        activityList.insertBefore(activityItem, activityList.firstChild)

        // Limit to 5 activities
        if (activityList.children.length > 5) {
            activityList.removeChild(activityList.lastChild)
        }
    }

    // Project Functions
    function setupProjectListeners() {
        addProjectBtn.addEventListener("click", () => {
            document.getElementById("projectModalTitle").textContent = "Adicionar Projeto"
            projectForm.reset()
            document.getElementById("projectId").value = ""
            projectModal.style.display = "block"
        })

        projectForm.addEventListener("submit", (e) => {
            e.preventDefault()
            saveProject()
        })
    }

    function renderProjects() {
        projectsTable.innerHTML = ""

        projects.forEach((project, index) => {
            const row = document.createElement("tr")
            row.innerHTML = `
                <td>${project.title}</td>
                <td>${getCategoryName(project.category)}</td>
                <td>${project.status}</td>
                <td>
                    <div class="table-actions">
                        <button class="edit-btn" data-id="${index}">✏️ Editar</button>
                        <button class="delete-btn" data-id="${index}">🗑️ Excluir</button>
                    </div>
                </td>
            `

            projectsTable.appendChild(row)
        })

        // Add event listeners to buttons
        projectsTable.querySelectorAll(".edit-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                editProject(Number.parseInt(this.getAttribute("data-id")))
            })
        })

        projectsTable.querySelectorAll(".delete-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                deleteProject(Number.parseInt(this.getAttribute("data-id")))
            })
        })
    }

    function saveProject() {
        const id = document.getElementById("projectId").value
        const project = {
            title: document.getElementById("projectTitle").value,
            category: document.getElementById("projectCategory").value,
            description: document.getElementById("projectDescription").value,
            status: document.getElementById("projectStatus").value,
            image: document.getElementById("projectImage").value,
        }

        if (id === "") {
            // Add new project
            projects.push(project)
            addActivity(`Projeto "${project.title}" adicionado`)
        } else {
            // Update existing project
            projects[Number.parseInt(id)] = project
            addActivity(`Projeto "${project.title}" atualizado`)
        }

        localStorage.setItem("projects", JSON.stringify(projects))
        renderProjects()
        updateCounters()
        projectModal.style.display = "none"
    }

    function editProject(id) {
        const project = projects[id]
        document.getElementById("projectModalTitle").textContent = "Editar Projeto"
        document.getElementById("projectId").value = id
        document.getElementById("projectTitle").value = project.title
        document.getElementById("projectCategory").value = project.category
        document.getElementById("projectDescription").value = project.description
        document.getElementById("projectStatus").value = project.status
        document.getElementById("projectImage").value = project.image

        projectModal.style.display = "block"
    }

    function deleteProject(id) {
        if (confirm("Tem certeza que deseja excluir este projeto?")) {
            const projectName = projects[id].title
            projects.splice(id, 1)
            localStorage.setItem("projects", JSON.stringify(projects))
            renderProjects()
            updateCounters()
            addActivity(`Projeto "${projectName}" excluído`)
        }
    }

    function getCategoryName(category) {
        const categories = {
            conservation: "Conservação",
            sustainability: "Sustentabilidade",
            water: "Recursos Hídricos",
        }
        return categories[category] || category
    }

    // Experience Functions
    function setupExperienceListeners() {
        addExperienceBtn.addEventListener("click", () => {
            document.getElementById("experienceModalTitle").textContent = "Adicionar Experiência"
            experienceForm.reset()
            document.getElementById("experienceId").value = ""
            experienceModal.style.display = "block"
        })

        experienceForm.addEventListener("submit", (e) => {
            e.preventDefault()
            saveExperience()
        })
    }

    function renderExperiences() {
        experienceTable.innerHTML = ""

        experiences.forEach((exp, index) => {
            const row = document.createElement("tr")
            row.innerHTML = `
                <td>${exp.jobTitle}</td>
                <td>${exp.company}</td>
                <td>${exp.startDate} - ${exp.endDate}</td>
                <td>
                    <div class="table-actions">
                        <button class="edit-btn" data-id="${index}">✏️ Editar</button>
                        <button class="delete-btn" data-id="${index}">🗑️ Excluir</button>
                    </div>
                </td>
            `

            experienceTable.appendChild(row)
        })

        // Add event listeners to buttons
        experienceTable.querySelectorAll(".edit-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                editExperience(Number.parseInt(this.getAttribute("data-id")))
            })
        })

        experienceTable.querySelectorAll(".delete-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                deleteExperience(Number.parseInt(this.getAttribute("data-id")))
            })
        })
    }

    function saveExperience() {
        const id = document.getElementById("experienceId").value
        const experience = {
            jobTitle: document.getElementById("jobTitle").value,
            company: document.getElementById("company").value,
            startDate: document.getElementById("startDate").value,
            endDate: document.getElementById("endDate").value || "Presente",
            description: document.getElementById("jobDescription").value,
            achievements: document
                .getElementById("achievements")
                .value.split("\n")
                .filter((item) => item.trim() !== ""),
        }

        if (id === "") {
            // Add new experience
            experiences.push(experience)
            addActivity(`Experiência em "${experience.company}" adicionada`)
        } else {
            // Update existing experience
            experiences[Number.parseInt(id)] = experience
            addActivity(`Experiência em "${experience.company}" atualizada`)
        }

        localStorage.setItem("experiences", JSON.stringify(experiences))
        renderExperiences()
        updateCounters()
        experienceModal.style.display = "none"
    }

    function editExperience(id) {
        const exp = experiences[id]
        document.getElementById("experienceModalTitle").textContent = "Editar Experiência"
        document.getElementById("experienceId").value = id
        document.getElementById("jobTitle").value = exp.jobTitle
        document.getElementById("company").value = exp.company
        document.getElementById("startDate").value = exp.startDate
        document.getElementById("endDate").value = exp.endDate === "Presente" ? "" : exp.endDate
        document.getElementById("jobDescription").value = exp.description
        document.getElementById("achievements").value = exp.achievements.join("\n")

        experienceModal.style.display = "block"
    }

    function deleteExperience(id) {
        if (confirm("Tem certeza que deseja excluir esta experiência?")) {
            const expName = experiences[id].company
            experiences.splice(id, 1)
            localStorage.setItem("experiences", JSON.stringify(experiences))
            renderExperiences()
            updateCounters()
            addActivity(`Experiência em "${expName}" excluída`)
        }
    }

    // Education Functions
    function setupEducationListeners() {
        addEducationBtn.addEventListener("click", () => {
            document.getElementById("educationModalTitle").textContent = "Adicionar Formação"
            educationForm.reset()
            document.getElementById("educationId").value = ""
            educationModal.style.display = "block"
        })

        educationForm.addEventListener("submit", (e) => {
            e.preventDefault()
            saveEducation()
        })
    }

    function renderEducation() {
        educationTable.innerHTML = ""

        education.forEach((edu, index) => {
            const row = document.createElement("tr")
            row.innerHTML = `
                <td>${edu.degree}</td>
                <td>${edu.institution}</td>
                <td>${edu.startDate} - ${edu.endDate}</td>
                <td>
                    <div class="table-actions">
                        <button class="edit-btn" data-id="${index}">✏️ Editar</button>
                        <button class="delete-btn" data-id="${index}">🗑️ Excluir</button>
                    </div>
                </td>
            `

            educationTable.appendChild(row)
        })

        // Add event listeners to buttons
        educationTable.querySelectorAll(".edit-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                editEducation(Number.parseInt(this.getAttribute("data-id")))
            })
        })

        educationTable.querySelectorAll(".delete-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                deleteEducation(Number.parseInt(this.getAttribute("data-id")))
            })
        })
    }

    function saveEducation() {
        const id = document.getElementById("educationId").value
        const edu = {
            degree: document.getElementById("degree").value,
            institution: document.getElementById("institution").value,
            startDate: document.getElementById("eduStartDate").value,
            endDate: document.getElementById("eduEndDate").value || "Presente",
            description: document.getElementById("eduDescription").value,
            type: document.getElementById("eduType").value,
        }

        if (id === "") {
            // Add new education
            education.push(edu)
            addActivity(`Formação em "${edu.degree}" adicionada`)
        } else {
            // Update existing education
            education[Number.parseInt(id)] = edu
            addActivity(`Formação em "${edu.degree}" atualizada`)
        }

        localStorage.setItem("education", JSON.stringify(education))
        renderEducation()
        updateCounters()
        educationModal.style.display = "none"
    }

    function editEducation(id) {
        const edu = education[id]
        document.getElementById("educationModalTitle").textContent = "Editar Formação"
        document.getElementById("educationId").value = id
        document.getElementById("degree").value = edu.degree
        document.getElementById("institution").value = edu.institution
        document.getElementById("eduStartDate").value = edu.startDate
        document.getElementById("eduEndDate").value = edu.endDate === "Presente" ? "" : edu.endDate
        document.getElementById("eduDescription").value = edu.description
        document.getElementById("eduType").value = edu.type

        educationModal.style.display = "block"
    }

    function deleteEducation(id) {
        if (confirm("Tem certeza que deseja excluir esta formação?")) {
            const eduName = education[id].degree
            education.splice(id, 1)
            localStorage.setItem("education", JSON.stringify(education))
            renderEducation()
            updateCounters()
            addActivity(`Formação em "${eduName}" excluída`)
        }
    }

    // Skills Functions
    function setupSkillListeners() {
        addSkillBtn.addEventListener("click", () => {
            document.getElementById("skillModalTitle").textContent = "Adicionar Habilidade"
            skillForm.reset()
            document.getElementById("skillId").value = ""
            skillLevel.value = 80
            skillLevelOutput.textContent = "80%"
            skillModal.style.display = "block"
        })

        skillForm.addEventListener("submit", (e) => {
            e.preventDefault()
            saveSkill()
        })

        skillLevel.addEventListener("input", function () {
            skillLevelOutput.textContent = this.value + "%"
        })
    }

    function renderSkills() {
        skillsTable.innerHTML = ""

        skills.forEach((skill, index) => {
            const row = document.createElement("tr")
            row.innerHTML = `
                <td>${skill.name}</td>
                <td>
                    <div class="skill-bar" style="width: 150px; height: 10px; background-color: #e0e0e0; border-radius: 5px;">
                        <div style="width: ${skill.level}%; height: 100%; background-color: var(--primary-color); border-radius: 5px;"></div>
                    </div>
                    <span style="margin-left: 10px;">${skill.level}%</span>
                </td>
                <td>${getSkillCategoryName(skill.category)}</td>
                <td>
                    <div class="table-actions">
                        <button class="edit-btn" data-id="${index}">✏️ Editar</button>
                        <button class="delete-btn" data-id="${index}">🗑️ Excluir</button>
                    </div>
                </td>
            `

            skillsTable.appendChild(row)
        })

        // Add event listeners to buttons
        skillsTable.querySelectorAll(".edit-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                editSkill(Number.parseInt(this.getAttribute("data-id")))
            })
        })

        skillsTable.querySelectorAll(".delete-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                deleteSkill(Number.parseInt(this.getAttribute("data-id")))
            })
        })
    }

    function saveSkill() {
        const id = document.getElementById("skillId").value
        const skill = {
            name: document.getElementById("skillName").value,
            level: Number.parseInt(document.getElementById("skillLevel").value),
            category: document.getElementById("skillCategory").value,
        }

        if (id === "") {
            // Add new skill
            skills.push(skill)
            addActivity(`Habilidade "${skill.name}" adicionada`)
        } else {
            // Update existing skill
            skills[Number.parseInt(id)] = skill
            addActivity(`Habilidade "${skill.name}" atualizada`)
        }

        localStorage.setItem("skills", JSON.stringify(skills))
        renderSkills()
        updateCounters()
        skillModal.style.display = "none"
    }

    function editSkill(id) {
        const skill = skills[id]
        document.getElementById("skillModalTitle").textContent = "Editar Habilidade"
        document.getElementById("skillId").value = id
        document.getElementById("skillName").value = skill.name
        document.getElementById("skillLevel").value = skill.level
        document.getElementById("skillLevelOutput").textContent = skill.level + "%"
        document.getElementById("skillCategory").value = skill.category

        skillModal.style.display = "block"
    }

    function deleteSkill(id) {
        if (confirm("Tem certeza que deseja excluir esta habilidade?")) {
            const skillName = skills[id].name
            skills.splice(id, 1)
            localStorage.setItem("skills", JSON.stringify(skills))
            renderSkills()
            updateCounters()
            addActivity(`Habilidade "${skillName}" excluída`)
        }
    }

    function getSkillCategoryName(category) {
        const categories = {
            environmental: "Gestão Ambiental",
            technical: "Habilidades Técnicas",
            water: "Recursos Hídricos",
            other: "Outras",
        }
        return categories[category] || category
    }

    // Contact Functions
    function setupContactListeners() {
        contactForm.addEventListener("submit", (e) => {
            e.preventDefault()
            saveContactInfo()
        })

        addSocialBtn.addEventListener("click", () => {
            addSocialLinkField()
        })

        // Setup initial remove buttons
        setupRemoveSocialButtons()
    }

    function loadContactInfo() {
        document.getElementById("fullName").value = contactInfo.fullName
        document.getElementById("title").value = contactInfo.title
        document.getElementById("email").value = contactInfo.email
        document.getElementById("phone").value = contactInfo.phone
        document.getElementById("location").value = contactInfo.location
        document.getElementById("aboutMe").value = contactInfo.aboutMe

        // Clear existing social links
        socialLinks.innerHTML = ""

        // Add social links
        contactInfo.socialLinks.forEach((link, index) => {
            addSocialLinkField(link.type, link.url)
        })
    }

    function saveContactInfo() {
        contactInfo = {
            fullName: document.getElementById("fullName").value,
            title: document.getElementById("title").value,
            email: document.getElementById("email").value,
            phone: document.getElementById("phone").value,
            location: document.getElementById("location").value,
            aboutMe: document.getElementById("aboutMe").value,
            socialLinks: [],
        }

        // Get all social links
        const socialTypes = document.querySelectorAll(".social-type")
        const socialUrls = document.querySelectorAll(".social-url")

        for (let i = 0; i < socialTypes.length; i++) {
            contactInfo.socialLinks.push({
                type: socialTypes[i].value,
                url: socialUrls[i].value,
            })
        }

        localStorage.setItem("contactInfo", JSON.stringify(contactInfo))
        addActivity("Informações de contato atualizadas")
        alert("Informações de contato salvas com sucesso!")
    }

    function addSocialLinkField(type = "linkedin", url = "") {
        const index = document.querySelectorAll(".social-link-item").length

        const socialLinkItem = document.createElement("div")
        socialLinkItem.className = "social-link-item"
        socialLinkItem.innerHTML = `
            <div class="form-group">
                <label for="socialType${index}">Rede Social</label>
                <select id="socialType${index}" class="social-type">
                    <option value="linkedin" ${type === "linkedin" ? "selected" : ""}>LinkedIn</option>
                    <option value="github" ${type === "github" ? "selected" : ""}>GitHub</option>
                    <option value="twitter" ${type === "twitter" ? "selected" : ""}>Twitter</option>
                    <option value="instagram" ${type === "instagram" ? "selected" : ""}>Instagram</option>
                    <option value="facebook" ${type === "facebook" ? "selected" : ""}>Facebook</option>
                </select>
            </div>
            <div class="form-group">
                <label for="socialUrl${index}">URL</label>
                <input type="url" id="socialUrl${index}" class="social-url" value="${url}">
            </div>
            <button type="button" class="remove-btn">Remover</button>
        `

        socialLinks.appendChild(socialLinkItem)
        setupRemoveSocialButtons()
    }

    function setupRemoveSocialButtons() {
        document.querySelectorAll(".remove-btn").forEach((btn) => {
            btn.addEventListener("click", function () {
                if (document.querySelectorAll(".social-link-item").length > 1) {
                    this.parentElement.remove()
                } else {
                    alert("Você deve manter pelo menos uma rede social.")
                }
            })
        })
    }

    // Settings Functions
    function setupSettingsListeners() {
        exportDataBtn.addEventListener("click", exportData)
        importDataBtn.addEventListener("click", () => {
            importFile.click()
        })

        importFile.addEventListener("change", importData)

        updatePasswordBtn.addEventListener("click", () => {
            const currentPassword = document.getElementById("currentPassword").value
            const newPassword = document.getElementById("newPassword").value
            const confirmPassword = document.getElementById("confirmPassword").value

            if (!currentPassword || !newPassword || !confirmPassword) {
                alert("Por favor, preencha todos os campos de senha.")
                return
            }

            if (newPassword !== confirmPassword) {
                alert("A nova senha e a confirmação não coincidem.")
                return
            }

            // In a real application, you would validate the current password
            // and update the password in a secure way
            alert("Senha atualizada com sucesso!")
            document.getElementById("currentPassword").value = ""
            document.getElementById("newPassword").value = ""
            document.getElementById("confirmPassword").value = ""
        })

        primaryColorInput.addEventListener("change", updateColors)
        secondaryColorInput.addEventListener("change", updateColors)

        defaultThemeSelect.addEventListener("change", function () {
            if (this.value === "dark") {
                body.classList.add("dark-theme")
            } else {
                body.classList.remove("dark-theme")
            }
            localStorage.setItem("theme", this.value)
        })
    }

    function exportData() {
        const data = {
            projects,
            experiences,
            education,
            skills,
            contactInfo,
        }

        const dataStr = JSON.stringify(data, null, 2)
        const dataUri = "data:application/json;charset=utf-8," + encodeURIComponent(dataStr)

        const exportFileDefaultName = "portfolio-data.json"

        const linkElement = document.createElement("a")
        linkElement.setAttribute("href", dataUri)
        linkElement.setAttribute("download", exportFileDefaultName)
        linkElement.click()

        addActivity("Dados exportados")
    }

    function importData(e) {
        const file = e.target.files[0]
        if (!file) return

        const reader = new FileReader()
        reader.onload = (e) => {
            try {
                const data = JSON.parse(e.target.result)

                if (data.projects) {
                    projects = data.projects
                    localStorage.setItem("projects", JSON.stringify(projects))
                }

                if (data.experiences) {
                    experiences = data.experiences
                    localStorage.setItem("experiences", JSON.stringify(experiences))
                }

                if (data.education) {
                    education = data.education
                    localStorage.setItem("education", JSON.stringify(education))
                }

                if (data.skills) {
                    skills = data.skills
                    localStorage.setItem("skills", JSON.stringify(skills))
                }

                if (data.contactInfo) {
                    contactInfo = data.contactInfo
                    localStorage.setItem("contactInfo", JSON.stringify(contactInfo))
                }

                loadData()
                updateCounters()
                addActivity("Dados importados")
                alert("Dados importados com sucesso!")
            } catch (error) {
                alert("Erro ao importar dados: " + error.message)
            }
        }
        reader.readAsText(file)
    }

    function updateColors() {
        document.documentElement.style.setProperty("--primary-color", primaryColorInput.value)
        document.documentElement.style.setProperty("--secondary-color", secondaryColorInput.value)

        // Save colors to localStorage
        localStorage.setItem("primaryColor", primaryColorInput.value)
        localStorage.setItem("secondaryColor", secondaryColorInput.value)
    }

    // Initialize the application
    init()
})

