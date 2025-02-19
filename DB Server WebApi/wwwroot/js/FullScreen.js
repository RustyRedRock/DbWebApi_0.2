// fullscreen.js

window.fullscreenJsFunctions = {
    requestFullscreen: function (element) {
        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.mozRequestFullScreen) {
            element.mozRequestFullScreen();
        } else if (element.webkitRequestFullscreen) {
            element.webkitRequestFullscreen();
        } else if (element.msRequestFullscreen) {
            element.msRequestFullscreen();
        }
    },
    exitFullscreen: function () {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.mozCancelFullScreen) {
            document.mozCancelFullScreen();
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
    },
    isFullscreen: function () {
        return !!document.fullscreenElement;
    },
    addFullscreenChangeListener: function (dotNetObjectReference) {
        // Utilisez une fonction fléchée pour conserver le contexte de this
        const handler = () => {
            dotNetObjectReference.invokeMethodAsync('OnFullscreenChange', this.isFullscreen());
        };

        document.addEventListener('fullscreenchange', handler);
        document.addEventListener('webkitfullscreenchange', handler); // Chrome, Safari
        document.addEventListener('mozfullscreenchange', handler); // Firefox
        document.addEventListener('MSFullscreenChange', handler); // IE/Edge

        // Retourne un objet qui permet de supprimer les écouteurs plus tard
        return {
            dispose: () => {
                document.removeEventListener('fullscreenchange', handler);
                document.removeEventListener('webkitfullscreenchange', handler);
                document.removeEventListener('mozfullscreenchange', handler);
                document.removeEventListener('MSFullscreenChange', handler);
            }
        };
    }
};