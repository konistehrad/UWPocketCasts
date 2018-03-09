(function () {
    const imageSelector = ".player-image .podcast-image .image-loaded";
    const episodeTitleSelector = ".episode .episode-title";
    const podcastTitleSelector = ".podcast-title-date .podcast-title";

    var foundAudio;

    function onEventPlaying(e) { window.external.notify('playing'); }
    function onEventPause(e) { window.external.notify('pause'); }
    function onEventAbort(e) { window.external.notify('abort'); }
    function onEventEnded(e) { window.external.notify('ended'); }
    function onEventSeeked(e) { window.external.notify('seeked'); }

    function unregisterCallbacks(e) {
        if (!e) return;
        e.removeEventListener('playing', onEventPlaying);
        e.removeEventListener('pause', onEventPause);
        e.removeEventListener('abort', onEventAbort);
        e.removeEventListener('ended', onEventEnded);
        e.removeEventListener('seeked', onEventSeeked);
    }

    function registerCallbacks(e) {
        if (!e) return;
        e.addEventListener('playing', onEventPlaying);
        e.addEventListener('pause', onEventPause);
        e.addEventListener('abort', onEventAbort);
        e.addEventListener('ended', onEventEnded);
        e.addEventListener('seeked', onEventSeeked);
    }

    function jsWatch() {
        try {
            var currentFoundAudioEl = document.querySelector('audio');
            if (foundAudio !== currentFoundAudioEl) {
                if (foundAudio) {
                    unregisterCallbacks(foundAudio);
                }

                foundAudio = currentFoundAudioEl;

                if (foundAudio) {
                    window.external.notify('audioFound');
                    registerCallbacks(foundAudio);
                }
                else {
                    window.external.notify('audioLost');
                }
            }
        }
        catch (e) {
        }

        setTimeout(jsWatch, 500);
    }

    window.pocketCastBridge = {
        get isPlaying() {
            return foundAudio && foundAudio.duration > 0 && !foundAudio.paused;
        },

        get durationInSeconds() {
            return foundAudio ? foundAudio.duration : 0;
        },

        get positionInSeconds() {
            return foundAudio ? foundAudio.currentTime : 0;
        }, 

        get episodeTitle() {
            var el = document.querySelector(episodeTitleSelector);
            return el ? el.innerText : "";
        },

        get podcastTitle() {
            var el = document.querySelector(podcastTitleSelector);
            return el ? el.innerText : "";
        },

        get jsonPlayerState() {
            return JSON.stringify({
                isPlaying: this.isPlaying,
                durationInSeconds: this.durationInSeconds,
                positionInSeconds: this.positionInSeconds,
                episodeTitle: this.episodeTitle,
                podcastTitle: this.podcastTitle,
            });
        },

        pause() {
            foundAudio && foundAudio.pause();
        },

        play() {
            foundAudio && foundAudio.play();
        },
    };

    setTimeout(jsWatch, 500);


    return 'loaded';
})();
