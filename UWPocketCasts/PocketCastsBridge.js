(function () {
    const pollTimeInMs = 250;
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

            // tick tock don't stop
            // notify the wrapper it's time to take a look at the player state
            window.external.notify('tick');
        }
        catch (e) {
        }

        setTimeout(jsWatch, pollTimeInMs);
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

        set positionInSeconds(value) {
            // no audio, no good
            if (!foundAudio) {
                return;
            }

            // try parsing input; if we fail, bail
            // (this way accepts strings and floats)
            value = Number.parseFloat(value);
            if (Number.isNaN(value)) {
                return;
            }

            // we made it this far, do the assignment!
            foundAudio.currentTime = value;
        },

        get episodeTitle() {
            var el = document.querySelector(episodeTitleSelector);
            return el ? el.innerText : "";
        },

        get podcastTitle() {
            var el = document.querySelector(podcastTitleSelector);
            return el ? el.innerText : "";
        },

        get podcastImageURL() {
            var el = document.querySelector(imageSelector);
            return el ? el.src : "";
        },

        get jsonPlayerState() {
            return JSON.stringify({
                isPlaying: this.isPlaying,
                durationInSeconds: this.durationInSeconds,
                positionInSeconds: this.positionInSeconds,
                episodeTitle: this.episodeTitle,
                podcastTitle: this.podcastTitle,
                podcastImageURL: this.podcastImageURL,
            });
        },

        pause() {
            foundAudio && foundAudio.pause();
        },

        play() {
            foundAudio && foundAudio.play();
        },
    };

    setTimeout(jsWatch, pollTimeInMs);
    
    return 'loaded';
})();
