// File upload helper functions
window.DocNFileUpload = {
    triggerFileInput: function (elementId) {
        try {
            // Validate elementId to prevent CSS injection
            if (!elementId || typeof elementId !== 'string' || !/^[a-zA-Z0-9_-]+$/.test(elementId)) {
                console.error('Invalid element ID provided');
                return false;
            }
            
            const fileInput = document.querySelector(`#${elementId} input[type="file"]`);
            if (fileInput) {
                fileInput.click();
                return true;
            }
            console.warn(`File input not found for element: ${elementId}`);
            return false;
        } catch (error) {
            console.error('Error triggering file input:', error);
            return false;
        }
    }
};
