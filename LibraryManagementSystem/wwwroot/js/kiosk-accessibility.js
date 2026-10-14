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
            // Controls remain usable when storage is unavailable.
        }

        const style = document.createElement("style");
        style.id = "kiosk-accessibility-styles";

        style.textContent = `
            html.kiosk-large-text {
                font-size: 125%;
            }

            #kiosk-accessibility {
                display: flex;
                align-items: center;
                flex-wrap: wrap;
                gap: 12px;
                padding: 16px clamp(20px, 5vw, 72px);
                border-bottom: 1px solid #e5e0d6;
                background: #fffdf8;
                color: #34332e;
            }

            #kiosk-accessibility .accessibility-label {
                margin-right: 8px;
                color: #65635e;
                font-weight: 750;
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
                border: 2px solid #987027;
                border-radius: 10px;
                background: #fffdf8;
                color: #705016;
                font: inherit;
                font-weight: 700;
                cursor: pointer;
            }

            #kiosk-accessibility button:hover {
                background: #f8efd9;
            }

            #kiosk-accessibility button[aria-pressed="true"] {
                border-color: #20211f;
                background: #20211f;
                color: #e9bc62;
            }

            button,
            a.button {
                transform-origin: center;
                transition:
                    transform 160ms ease,
                    box-shadow 160ms ease,
                    background-color 160ms ease;
            }

            @media (hover: hover) and (pointer: fine) {
                button:not(:disabled):hover,
                a.button:hover {
                    transform: scale(1.04);
                    box-shadow: 0 8px 20px rgba(32, 33, 31, .16);
                }

                button:not(:disabled):active,
                a.button:active {
                    transform: scale(.98);
                }

                input:not([type="hidden"]):hover,
                select:hover,
                textarea:hover {
                    outline: 2px solid #916216;
                    outline-offset: 3px;
                }

                a:not(.button):hover {
                    text-decoration: underline;
                    text-underline-offset: 4px;
                }
            }

            button:focus-visible,
            a:focus-visible,
            input:focus-visible,
            select:focus-visible,
            textarea:focus-visible {
                outline: 3px solid #916216;
                outline-offset: 5px;
            }

            /*
             * Apply a consistent dark surface to every kiosk panel.
             * The welcome page uses entry-card and welcome-panel,
             * while the account and borrowing pages use other classes.
             */
            html.kiosk-high-contrast,
            html.kiosk-high-contrast body,
            html.kiosk-high-contrast header,
            html.kiosk-high-contrast footer,
            html.kiosk-high-contrast main,
            html.kiosk-high-contrast .kiosk-header,
            html.kiosk-high-contrast .kiosk-main,
            html.kiosk-high-contrast .kiosk-footer,
            html.kiosk-high-contrast .welcome-panel,
            html.kiosk-high-contrast .entry-card,
            html.kiosk-high-contrast .welcome,
            html.kiosk-high-contrast .card,
            html.kiosk-high-contrast .workspace,
            html.kiosk-high-contrast .guide,
            html.kiosk-high-contrast .entry,
            html.kiosk-high-contrast .item-summary,
            html.kiosk-high-contrast .notice,
            html.kiosk-high-contrast .support,
            html.kiosk-high-contrast dialog,
            html.kiosk-high-contrast #kiosk-accessibility {
                background: #000 !important;
                color: #fff !important;
                border-color: #fff !important;
                box-shadow: none !important;
            }

            /*
             * Explicitly override page-specific text colours.
             * This prevents white text on the previous white card,
             * or dark labels remaining on a black background.
             */
            html.kiosk-high-contrast h1,
            html.kiosk-high-contrast h2,
            html.kiosk-high-contrast h3,
            html.kiosk-high-contrast h4,
            html.kiosk-high-contrast h5,
            html.kiosk-high-contrast h6,
            html.kiosk-high-contrast h1 span,
            html.kiosk-high-contrast h1 em,
            html.kiosk-high-contrast p,
            html.kiosk-high-contrast label,
            html.kiosk-high-contrast dt,
            html.kiosk-high-contrast dd,
            html.kiosk-high-contrast time,
            html.kiosk-high-contrast .muted,
            html.kiosk-high-contrast .help,
            html.kiosk-high-contrast .intro,
            html.kiosk-high-contrast .eyebrow,
            html.kiosk-high-contrast .brand,
            html.kiosk-high-contrast .brand-name,
            html.kiosk-high-contrast .brand-name span,
            html.kiosk-high-contrast .brand small,
            html.kiosk-high-contrast .welcome-copy,
            html.kiosk-high-contrast .guide-copy,
            html.kiosk-high-contrast .step-copy,
            html.kiosk-high-contrast .step-copy strong,
            html.kiosk-high-contrast .step-title,
            html.kiosk-high-contrast .step-note,
            html.kiosk-high-contrast .metric-label,
            html.kiosk-high-contrast .metric-note,
            html.kiosk-high-contrast .value,
            html.kiosk-high-contrast .due-label,
            html.kiosk-high-contrast .due-date,
            html.kiosk-high-contrast .item-code,
            html.kiosk-high-contrast .support strong,
            html.kiosk-high-contrast .privacy-note,
            html.kiosk-high-contrast .notice,
            html.kiosk-high-contrast
                #kiosk-accessibility .accessibility-label {
                color: #fff !important;
            }

            html.kiosk-high-contrast .entry-card,
            html.kiosk-high-contrast .welcome-panel,
            html.kiosk-high-contrast .welcome,
            html.kiosk-high-contrast .workspace,
            html.kiosk-high-contrast .card,
            html.kiosk-high-contrast .item-summary,
            html.kiosk-high-contrast .notice,
            html.kiosk-high-contrast dialog {
                border: 2px solid #fff !important;
            }

            html.kiosk-high-contrast .steps,
            html.kiosk-high-contrast .support {
                border-color: #fff !important;
            }

            html.kiosk-high-contrast .brand-mark,
            html.kiosk-high-contrast .brand-icon,
            html.kiosk-high-contrast .card-icon,
            html.kiosk-high-contrast .scan-symbol,
            html.kiosk-high-contrast .metric-icon,
            html.kiosk-high-contrast .item-mark,
            html.kiosk-high-contrast .help-icon,
            html.kiosk-high-contrast .step-number,
            html.kiosk-high-contrast .number,
            html.kiosk-high-contrast .status,
            html.kiosk-high-contrast .badge,
            html.kiosk-high-contrast .kiosk-badge {
                border: 2px solid #fff !important;
                background: #000 !important;
                color: #fff !important;
                box-shadow: none !important;
            }

            html.kiosk-high-contrast
                .steps [aria-current="step"] .step-number {
                border-color: #ffff00 !important;
                background: #ffff00 !important;
                color: #000 !important;
            }

            html.kiosk-high-contrast a {
                color: #ffff00 !important;
            }

            html.kiosk-high-contrast button,
            html.kiosk-high-contrast .button,
            html.kiosk-high-contrast .start-button,
            html.kiosk-high-contrast #kiosk-accessibility button {
                border: 2px solid #ffff00 !important;
                background: #ffff00 !important;
                color: #000 !important;
                box-shadow: none !important;
            }

            html.kiosk-high-contrast button span,
            html.kiosk-high-contrast .button span {
                color: inherit !important;
            }

            html.kiosk-high-contrast
                #kiosk-accessibility button[aria-pressed="true"] {
                border-color: #ffff00 !important;
                background: #000 !important;
                color: #ffff00 !important;
            }

            html.kiosk-high-contrast .skip-link {
                border: 2px solid #ffff00 !important;
                background: #ffff00 !important;
                color: #000 !important;
            }

            html.kiosk-high-contrast input:not([type="hidden"]),
            html.kiosk-high-contrast select,
            html.kiosk-high-contrast textarea {
                border: 2px solid #fff !important;
                background: #000 !important;
                color: #fff !important;
                caret-color: #ffff00 !important;
                box-shadow: none !important;
            }

            html.kiosk-high-contrast select option {
                background: #000 !important;
                color: #fff !important;
            }

            html.kiosk-high-contrast input::placeholder,
            html.kiosk-high-contrast textarea::placeholder {
                color: #ddd !important;
                opacity: 1;
            }

            html.kiosk-high-contrast .error,
            html.kiosk-high-contrast .field-validation-error,
            html.kiosk-high-contrast .validation-summary-errors,
            html.kiosk-high-contrast .validation-summary-errors li {
                color: #ffb4ab !important;
            }

            html.kiosk-high-contrast .validation-summary-errors {
                border-color: #ffb4ab !important;
                background: #000 !important;
            }

            html.kiosk-high-contrast dialog::backdrop {
                background: rgba(0, 0, 0, .85);
            }

            html.kiosk-high-contrast :focus-visible {
                outline: 3px solid #00ffff !important;
                outline-offset: 5px;
            }

            @media (hover: hover) and (pointer: fine) {
                html.kiosk-high-contrast button:not(:disabled):hover,
                html.kiosk-high-contrast a.button:hover {
                    outline: 2px solid #00ffff;
                    outline-offset: 4px;
                    box-shadow: none;
                }

                html.kiosk-high-contrast
                    input:not([type="hidden"]):hover,
                html.kiosk-high-contrast select:hover,
                html.kiosk-high-contrast textarea:hover {
                    outline-color: #00ffff !important;
                }
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

            @media (max-width: 420px) {
                #kiosk-accessibility {
                    gap: 10px;
                }

                #kiosk-accessibility .accessibility-label {
                    width: 100%;
                }
            }

            @media (prefers-reduced-motion: reduce) {
                html {
                    scroll-behavior: auto !important;
                }

                button,
                a.button {
                    transition: none !important;
                    transform: none !important;
                }

                button:hover,
                button:active,
                a.button:hover,
                a.button:active {
                    transform: none !important;
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
                // Changes still work without browser storage.
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

        const endSessionForm = document.getElementById("end-session");

        if (endSessionForm) {
            endSessionForm.addEventListener("submit", () => {
                try {
                    sessionStorage.removeItem(storageKey);
                } catch {
                    // Sign-out continues when storage is unavailable.
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