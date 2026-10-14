(() => {
    "use strict";

    function initialiseAccessibility() {
        if (document.getElementById("kiosk-accessibility")) {
            return;
        }

        const storageKey = "librarylink.kiosk.accessibility";
        const preferences = {
            largeText: false,
            highContrast: false
        };

        try {
            const saved = JSON.parse(
                sessionStorage.getItem(storageKey) || "{}"
            );

            preferences.largeText = saved?.largeText === true;
            preferences.highContrast = saved?.highContrast === true;
        } catch {
            // Controls still work if browser storage is unavailable.
        }

        const style = document.createElement("style");

        style.textContent = `
            html.kiosk-large-text {
                font-size: 125%;
            }

            #kiosk-accessibility {
                display: flex;
                flex-wrap: wrap;
                gap: 12px;
                align-items: center;
                padding: 16px clamp(20px, 5vw, 72px);
                border-bottom: 1px solid #d4ddd6;
                background: #fff;
                color: #173536;
            }

            #kiosk-accessibility .accessibility-label {
                font-weight: 750;
                margin-right: 8px;
            }

            #kiosk-accessibility button {
                display: inline-flex;
                align-items: center;
                justify-content: center;
                width: auto;
                min-width: 44px;
                min-height: 48px;
                margin: 0;
                padding: 10px 16px;
                border: 2px solid #075c56;
                border-radius: 10px;
                background: white;
                color: #075c56;
                font: inherit;
                font-weight: 700;
                cursor: pointer;
            }

            #kiosk-accessibility button[aria-pressed="true"] {
                background: #075c56;
                color: white;
            }

            #kiosk-accessibility button:focus-visible {
                outline: 3px solid #9b4700;
                outline-offset: 4px;
            }

            html.kiosk-high-contrast,
            html.kiosk-high-contrast body,
            html.kiosk-high-contrast header,
            html.kiosk-high-contrast footer,
            html.kiosk-high-contrast main,
            html.kiosk-high-contrast .card,
            html.kiosk-high-contrast .notice,
            html.kiosk-high-contrast dialog,
            html.kiosk-high-contrast #kiosk-accessibility {
                background: #000 !important;
                color: #fff !important;
                border-color: #fff !important;
                box-shadow: none !important;
            }

            html.kiosk-high-contrast .muted,
            html.kiosk-high-contrast .help,
            html.kiosk-high-contrast .intro,
            html.kiosk-high-contrast .eyebrow,
            html.kiosk-high-contrast .brand span,
            html.kiosk-high-contrast dt,
            html.kiosk-high-contrast .badge {
                color: #fff !important;
                border-color: #fff !important;
            }

            html.kiosk-high-contrast a {
                color: #ffff00 !important;
            }

            html.kiosk-high-contrast button,
            html.kiosk-high-contrast .button,
            html.kiosk-high-contrast #kiosk-accessibility button {
                background: #ffff00 !important;
                color: #000 !important;
                border: 2px solid #ffff00 !important;
            }

            html.kiosk-high-contrast
            #kiosk-accessibility button[aria-pressed="true"] {
                background: #000 !important;
                color: #ffff00 !important;
            }

            html.kiosk-high-contrast input:not([type="hidden"]) {
                background: #000 !important;
                color: #fff !important;
                border: 2px solid #fff !important;
            }

            html.kiosk-high-contrast .status,
            html.kiosk-high-contrast .number {
                background: #000 !important;
                color: #fff !important;
                border: 2px solid #fff !important;
            }

            html.kiosk-high-contrast .error,
            html.kiosk-high-contrast .field-validation-error {
                color: #ffb4ab !important;
            }

            html.kiosk-high-contrast :focus-visible {
                outline: 3px solid #00ffff !important;
                outline-offset: 5px;
            }

            .kiosk-sr-only {
                position: absolute;
                width: 1px;
                height: 1px;
                padding: 0;
                margin: -1px;
                overflow: hidden;
                clip-path: inset(50%);
                white-space: nowrap;
                border: 0;
            }

            @media (prefers-reduced-motion: reduce) {
                html {
                    scroll-behavior: auto !important;
                }
            }
        `;

        document.head.appendChild(style);

        const toolbar = document.createElement("section");
        toolbar.id = "kiosk-accessibility";
        toolbar.setAttribute("aria-label", "Display preferences");

        const label = document.createElement("span");
        label.className = "accessibility-label";
        label.textContent = "Display options";

        function createButton(text) {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = text;
            return button;
        }

        const textButton = createButton("Larger text");
        const contrastButton = createButton("High contrast");
        const resetButton = createButton("Reset display");

        const announcement = document.createElement("span");
        announcement.className = "kiosk-sr-only";
        announcement.setAttribute("role", "status");
        announcement.setAttribute("aria-atomic", "true");

        toolbar.append(
            label,
            textButton,
            contrastButton,
            resetButton,
            announcement
        );

        const header = document.querySelector("header");

        if (header) {
            header.insertAdjacentElement("afterend", toolbar);
        } else {
            document.body.prepend(toolbar);
        }

        function applyPreferences(message) {
            document.documentElement.classList.toggle(
                "kiosk-large-text",
                preferences.largeText
            );

            document.documentElement.classList.toggle(
                "kiosk-high-contrast",
                preferences.highContrast
            );

            textButton.setAttribute(
                "aria-pressed",
                String(preferences.largeText)
            );

            contrastButton.setAttribute(
                "aria-pressed",
                String(preferences.highContrast)
            );

            try {
                sessionStorage.setItem(
                    storageKey,
                    JSON.stringify(preferences)
                );
            } catch {
                // Display changes remain available without storage.
            }

            if (message) {
                announcement.textContent = message;
            }
        }

        textButton.addEventListener("click", () => {
            preferences.largeText = !preferences.largeText;

            applyPreferences(
                preferences.largeText
                    ? "Larger text enabled."
                    : "Standard text size restored."
            );
        });

        contrastButton.addEventListener("click", () => {
            preferences.highContrast = !preferences.highContrast;

            applyPreferences(
                preferences.highContrast
                    ? "High contrast enabled."
                    : "Standard colours restored."
            );
        });

        resetButton.addEventListener("click", () => {
            preferences.largeText = false;
            preferences.highContrast = false;

            applyPreferences("Default display restored.");
        });

        // Reset preferences when a patron finishes their session.
        const endSessionForm = document.getElementById("end-session");

        if (endSessionForm) {
            endSessionForm.addEventListener("submit", () => {
                try {
                    sessionStorage.removeItem(storageKey);
                } catch {
                    // Sign-out must continue even without storage.
                }
            });
        }

        applyPreferences();
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initialiseAccessibility,
            { once: true }
        );
    } else {
        initialiseAccessibility();
    }
})();