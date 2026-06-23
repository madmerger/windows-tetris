"use strict";

window.TetrisAudio = (() => {
    let bgm = null;
    let muted = false;
    const effects = {};

    function init() {
        bgm = new Audio("audio/tetris.mp3");
        bgm.loop = true;
        bgm.volume = 0.4;

        registerEffect("start", "audio/start.wav", 0.6);
        registerEffect("menu", "audio/menu_move.wav", 0.6);
        registerEffect("blip", "audio/blip.wav", 0.7);
        registerEffect("clear", "audio/tetris-win.mp3", 0.7);
    }

    function registerEffect(name, src, volume) {
        const audio = new Audio(src);
        audio.volume = volume;
        effects[name] = { src, volume, audio };
    }

    return {
        init() {
            init();
        },

        playBgm() {
            if (!bgm) return;
            bgm.currentTime = 0;
            bgm.volume = muted ? 0 : 0.4;
            bgm.play().catch(() => {});
        },

        stopBgm() {
            if (!bgm) return;
            bgm.pause();
            bgm.currentTime = 0;
        },

        pauseBgm() {
            if (bgm) bgm.pause();
        },

        resumeBgm() {
            if (bgm) bgm.play().catch(() => {});
        },

        playEffect(name) {
            if (muted) return;
            const entry = effects[name];
            if (!entry) return;
            const clone = new Audio(entry.src);
            clone.volume = entry.volume;
            clone.play().catch(() => {});
        },

        setMuted(value) {
            muted = value;
            if (bgm) {
                bgm.volume = muted ? 0 : 0.4;
            }
        }
    };
})();
