// Ayca Music - Audio Interop
let audio = null;
let dotNetRef = null;
let timeInterval = null;
let savedVolume = 0.7;
let mobileAudioUnlocked = false;
let pendingPlay = false;

// Mobile browsers require user gesture to unlock audio playback
function unlockMobileAudio() {
    if (!audio) return;
    
    // If there's a pending play request, execute it now (in user gesture context)
    if (pendingPlay && audio.src) {
        audio.play().then(function() {
            pendingPlay = false;
            mobileAudioUnlocked = true;
            console.log('Mobile audio: pending play succeeded');
        }).catch(function(e) {
            console.warn('Mobile audio: pending play failed:', e.message);
        });
        return;
    }
    
    if (mobileAudioUnlocked) return;
    
    // Try to unlock audio context by playing a silent moment
    audio.muted = true;
    var p = audio.play();
    if (p) {
        p.then(function() {
            audio.pause();
            audio.muted = false;
            audio.currentTime = 0;
            mobileAudioUnlocked = true;
            console.log('Mobile audio: unlocked successfully');
        }).catch(function() { 
            audio.muted = false; 
        });
    }
    
    // Also resume AudioContext if suspended
    if (typeof audioCtx !== 'undefined' && audioCtx && audioCtx.state === 'suspended') {
        audioCtx.resume();
    }
}

function registerMobileUnlock() {
    document.addEventListener('touchstart', unlockMobileAudio, { passive: true });
    document.addEventListener('touchend', unlockMobileAudio, { passive: true });
    document.addEventListener('click', unlockMobileAudio);
}
registerMobileUnlock();

window.audioInterop = {
    init: function (ref) {
        dotNetRef = ref;
        audio = new Audio();
        audio.volume = 0.7;
        audio.setAttribute('playsinline', '');
        audio.setAttribute('webkit-playsinline', '');

        audio.addEventListener('loadedmetadata', () => {
            dotNetRef.invokeMethodAsync('OnDurationChanged', audio.duration);
        });

        audio.addEventListener('ended', () => {
            dotNetRef.invokeMethodAsync('OnTrackEnded');
        });

        audio.addEventListener('play', () => {
            dotNetRef.invokeMethodAsync('OnPlayStateChanged', true);
            startTimeUpdates();
        });

        audio.addEventListener('pause', () => {
            dotNetRef.invokeMethodAsync('OnPlayStateChanged', false);
            stopTimeUpdates();
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            // Don't trigger if user is typing in an input
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

            switch (e.code) {
                case 'Space':
                    e.preventDefault();
                    if (audio.paused) audio.play().catch(() => { }); else audio.pause();
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    audio.currentTime = Math.min(audio.duration || 0, audio.currentTime + 5);
                    break;
                case 'ArrowLeft':
                    e.preventDefault();
                    audio.currentTime = Math.max(0, audio.currentTime - 5);
                    break;
                case 'ArrowUp':
                    e.preventDefault();
                    audio.volume = Math.min(1, audio.volume + 0.05);
                    updateVolumeFill();
                    break;
                case 'ArrowDown':
                    e.preventDefault();
                    audio.volume = Math.max(0, audio.volume - 0.05);
                    updateVolumeFill();
                    break;
                case 'KeyM':
                    e.preventDefault();
                    dotNetRef.invokeMethodAsync('OnMuteToggle');
                    break;
            }
        });
    },

    loadTrack: function (src) {
        if (audio) {
            audio.src = src;
            audio.load();
        }
    },

    play: function () {
        if (!audio) return;
        var p = audio.play();
        if (p) {
            p.catch(function(err) {
                console.warn('Play blocked, will retry on next tap:', err.message);
                pendingPlay = true;
            });
        }
    },

    pause: function () {
        if (audio) audio.pause();
        pendingPlay = false;
    },

    togglePlay: function () {
        if (!audio) return;
        if (audio.paused) {
            var p = audio.play();
            if (p) {
                p.catch(function(err) {
                    console.warn('Toggle play blocked:', err.message);
                    pendingPlay = true;
                });
            }
        } else {
            audio.pause();
            pendingPlay = false;
        }
    },

    seek: function (time) {
        if (audio) audio.currentTime = time;
    },

    seekPercent: function (pct) {
        if (audio && audio.duration) {
            audio.currentTime = pct * audio.duration;
        }
    },

    setVolume: function (vol) {
        if (audio) audio.volume = Math.max(0, Math.min(1, vol));
        updateVolumeFill();
    },

    getVolume: function () {
        return audio ? audio.volume : 0.7;
    },

    mute: function () {
        if (audio) {
            savedVolume = audio.volume;
            audio.volume = 0;
            updateVolumeFill();
        }
    },

    unmute: function () {
        if (audio) {
            audio.volume = savedVolume || 0.7;
            updateVolumeFill();
        }
    },

    dispose: function () {
        stopTimeUpdates();
        if (audio) {
            audio.pause();
            audio.src = '';
            audio = null;
        }
        dotNetRef = null;
    }
};

function updateVolumeFill() {
    const fill = document.getElementById('volumeFillEl');
    if (fill && audio) {
        fill.style.width = (audio.volume * 100) + '%';
    }
}

// ===== PROGRESS BAR SEEKING =====
window._seekBars = [];

window.initSeekBar = function (containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;

    let dragging = false;

    function getSeekFill() {
        return container.querySelector('.seek-fill');
    }
    function getSeekThumb() {
        return container.querySelector('.seek-thumb');
    }

    function applyVisual(pct) {
        const fill = getSeekFill();
        const thumb = getSeekThumb();
        if (fill) fill.style.width = (pct * 100) + '%';
        if (thumb) thumb.style.left = (pct * 100) + '%';
    }

    function seekFromEvent(clientX) {
        if (!audio || !audio.duration) return;
        const rect = container.getBoundingClientRect();
        const pct = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
        applyVisual(pct);
        audio.currentTime = pct * audio.duration;
    }

    container.addEventListener('mousedown', (e) => {
        dragging = true;
        e.preventDefault();
        e.stopPropagation();
        seekFromEvent(e.clientX);
    });

    document.addEventListener('mousemove', (e) => {
        if (dragging) seekFromEvent(e.clientX);
    });

    document.addEventListener('mouseup', () => {
        dragging = false;
    });

    // Touch support
    container.addEventListener('touchstart', (e) => {
        dragging = true;
        e.stopPropagation();
        seekFromEvent(e.touches[0].clientX);
    }, { passive: true });

    container.addEventListener('touchmove', (e) => {
        if (dragging) {
            e.preventDefault();
            seekFromEvent(e.touches[0].clientX);
        }
    }, { passive: false });

    container.addEventListener('touchend', () => {
        dragging = false;
    }, { passive: true });
};

// ===== VOLUME BAR =====
window.initVolumeBar = function (elementId, fillId) {
    const el = document.getElementById(elementId);
    const fill = document.getElementById(fillId);
    if (!el) return;

    function handleVolume(e) {
        if (!audio) return;
        const rect = el.getBoundingClientRect();
        const pct = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
        audio.volume = pct;
        savedVolume = pct;
        if (fill) fill.style.width = (pct * 100) + '%';
    }

    el.addEventListener('click', handleVolume);
    let dragging = false;
    el.addEventListener('mousedown', (e) => { dragging = true; handleVolume(e); });
    document.addEventListener('mousemove', (e) => { if (dragging) handleVolume(e); });
    document.addEventListener('mouseup', () => { dragging = false; });
};

function startTimeUpdates() {
    stopTimeUpdates();
    timeInterval = setInterval(() => {
        if (audio && dotNetRef) {
            dotNetRef.invokeMethodAsync('OnTimeUpdate', audio.currentTime);
        }
    }, 250);
}

function stopTimeUpdates() {
    if (timeInterval) {
        clearInterval(timeInterval);
        timeInterval = null;
    }
}

window.scrollLyricIntoView = function (index) {
    const container = document.getElementById('lyrics-scroll');
    if (!container) return;
    const lines = container.querySelectorAll('.lyric-line');
    if (lines[index]) {
        lines[index].scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
};

// ===== EQUALIZER (Web Audio API) =====
let audioCtx = null;
let sourceNode = null;
let bassFilter = null;
let midFilter = null;
let trebleFilter = null;

window.audioInterop.initEq = function () {
    try {
        if (audioCtx) return;
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        sourceNode = audioCtx.createMediaElementSource(audio);

        bassFilter = audioCtx.createBiquadFilter();
        bassFilter.type = 'lowshelf';
        bassFilter.frequency.value = 200;
        bassFilter.gain.value = 0;

        midFilter = audioCtx.createBiquadFilter();
        midFilter.type = 'peaking';
        midFilter.frequency.value = 1000;
        midFilter.Q.value = 1;
        midFilter.gain.value = 0;

        trebleFilter = audioCtx.createBiquadFilter();
        trebleFilter.type = 'highshelf';
        trebleFilter.frequency.value = 3000;
        trebleFilter.gain.value = 0;

        sourceNode.connect(bassFilter);
        bassFilter.connect(midFilter);
        midFilter.connect(trebleFilter);
        trebleFilter.connect(audioCtx.destination);
    } catch (e) {
        console.warn('EQ init failed:', e);
        // Fallback: connect directly
        if (sourceNode) sourceNode.connect(audioCtx.destination);
    }
};

window.audioInterop.setEq = function (bass, mid, treble) {
    if (bassFilter) bassFilter.gain.value = bass;
    if (midFilter) midFilter.gain.value = mid;
    if (trebleFilter) trebleFilter.gain.value = treble;
};

// ===== CROSSFADE =====
let crossfadeDuration = 0;

window.audioInterop.setCrossfade = function (seconds) {
    crossfadeDuration = seconds;
};

// ===== PAUSE (for sleep timer) =====
window.audioInterop.pause = function () {
    if (audio) {
        audio.pause();
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnPlayStateChanged', false);
    }
};

// ===== SEEK =====
window.audioInterop.seek = function (time) {
    if (audio) audio.currentTime = time;
};

// ===== THEME & ACCENT =====
window.audioInterop.setTheme = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('Musicly-theme', theme);
};

window.audioInterop.setAccent = function (accent) {
    document.documentElement.setAttribute('data-accent', accent);
    localStorage.setItem('Musicly-accent', accent);
};

window.audioInterop.getTheme = function () {
    const t = localStorage.getItem('Musicly-theme') || 'dark';
    document.documentElement.setAttribute('data-theme', t);
    return t;
};

window.audioInterop.getAccent = function () {
    const a = localStorage.getItem('Musicly-accent') || 'pink';
    document.documentElement.setAttribute('data-accent', a);
    return a;
};

// ===== KEYBOARD SHORTCUTS =====
document.addEventListener('keydown', function (e) {
    // Don't trigger when typing in inputs
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable) return;
    if (!dotNetRef) return;

    switch (e.key) {
        case ' ':
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'togglePlay');
            break;
        case 'n': case 'N':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'next');
            break;
        case 'p': case 'P':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'prev');
            break;
        case 'm': case 'M':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'mute');
            break;
        case 's': case 'S':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'shuffle');
            break;
        case 'r': case 'R':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'repeat');
            break;
        case 'l': case 'L':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'like');
            break;
        case 'q': case 'Q':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'queue');
            break;
        case '?':
            dotNetRef.invokeMethodAsync('OnShowShortcuts');
            break;
        case 'ArrowUp':
            e.preventDefault();
            if (audio) audio.volume = Math.min(1, audio.volume + 0.05);
            break;
        case 'ArrowDown':
            e.preventDefault();
            if (audio) audio.volume = Math.max(0, audio.volume - 0.05);
            break;
        case 'ArrowLeft':
            e.preventDefault();
            if (audio) audio.currentTime = Math.max(0, audio.currentTime - 5);
            break;
        case 'ArrowRight':
            e.preventDefault();
            if (audio) audio.currentTime = Math.min(audio.duration || 0, audio.currentTime + 5);
            break;
    }
});

// Apply saved theme on load
(function () {
    const t = localStorage.getItem('Musicly-theme');
    if (t) document.documentElement.setAttribute('data-theme', t);
    const a = localStorage.getItem('Musicly-accent');
    if (a) document.documentElement.setAttribute('data-accent', a);
})();

// ===== DOWNLOAD TRACK =====
window.audioInterop.downloadTrack = function (src, filename) {
    fetch(src)
        .then(response => response.blob())
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename.endsWith('.mp3') ? filename : filename + '.mp3';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        })
        .catch(err => console.error('Download failed:', err));
};

