"use strict";

const COLORS = [
    "#161C2B", // 0 empty
    "#22D3EE", // 1 I  cyan
    "#FACC15", // 2 O  yellow
    "#C084FC", // 3 T  purple
    "#4ADE80", // 4 S  green
    "#F87171", // 5 Z  red
    "#60A5FA", // 6 J  blue
    "#FB923C", // 7 L  orange
    "#64748B", // 8 wall slate
    "#FFC83D", // 9 gem gold
];

const EMPTY_COLOR = "#161C2B";
const STROKE_HIGHLIGHT = "rgba(255,255,255,0.24)";

const canvasContexts = {};

window.TetrisCanvas = {
    initCanvas(canvasId, cellSize) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        const ctx = canvas.getContext("2d");
        canvasContexts[canvasId] = { ctx, canvas };
    },

    drawBoard(canvasId, boardFlat, width, height, cellSize,
              activeCells, activeRow, activeCol, activeColor,
              ghostCells, ghostRow, ghostCol, ghostColor, ghostSize,
              activeSize, isPlaying) {
        const entry = canvasContexts[canvasId];
        if (!entry) return;
        const { ctx, canvas } = entry;

        canvas.width = width * cellSize;
        canvas.height = height * cellSize;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Locked cells + empty background grid
        for (let r = 0; r < height; r++) {
            for (let c = 0; c < width; c++) {
                const v = boardFlat[r * width + c];
                const x = c * cellSize + 1;
                const y = r * cellSize + 1;
                const s = cellSize - 2;

                if (v === 0) {
                    drawRoundRect(ctx, x, y, s, s, 5, EMPTY_COLOR, null);
                } else {
                    drawRoundRect(ctx, x, y, s, s, 5, COLORS[v], STROKE_HIGHLIGHT);
                    if (v === 9) {
                        drawGemDiamond(ctx, c * cellSize + cellSize / 2, r * cellSize + cellSize / 2, cellSize * 0.22);
                    }
                }
            }
        }

        // Ghost piece
        if (isPlaying && ghostCells) {
            for (let r = 0; r < ghostSize; r++) {
                for (let c = 0; c < ghostSize; c++) {
                    if (ghostCells[r * ghostSize + c] === 0) continue;
                    const br = ghostRow + r;
                    const bc = ghostCol + c;
                    if (br < 0 || br >= height) continue;
                    const x = bc * cellSize + 1;
                    const y = br * cellSize + 1;
                    const s = cellSize - 2;
                    ctx.save();
                    ctx.globalAlpha = 0.35;
                    ctx.strokeStyle = COLORS[ghostColor];
                    ctx.lineWidth = 2;
                    drawRoundRectPath(ctx, x, y, s, s, 5);
                    ctx.stroke();
                    ctx.restore();
                }
            }
        }

        // Active piece
        if (activeCells) {
            for (let r = 0; r < activeSize; r++) {
                for (let c = 0; c < activeSize; c++) {
                    if (activeCells[r * activeSize + c] === 0) continue;
                    const br = activeRow + r;
                    const bc = activeCol + c;
                    if (br < 0) continue;
                    const x = bc * cellSize + 1;
                    const y = br * cellSize + 1;
                    const s = cellSize - 2;
                    drawRoundRect(ctx, x, y, s, s, 5, COLORS[activeColor], STROKE_HIGHLIGHT);
                }
            }
        }
    },

    drawNext(canvasId, shapeCells, shapeSize, shapeColor, cellSize) {
        const entry = canvasContexts[canvasId];
        if (!entry) return;
        const { ctx, canvas } = entry;

        ctx.clearRect(0, 0, canvas.width, canvas.height);

        if (!shapeCells) return;

        let minR = shapeSize, minC = shapeSize, maxR = -1, maxC = -1;
        for (let r = 0; r < shapeSize; r++) {
            for (let c = 0; c < shapeSize; c++) {
                if (shapeCells[r * shapeSize + c] !== 0) {
                    minR = Math.min(minR, r);
                    maxR = Math.max(maxR, r);
                    minC = Math.min(minC, c);
                    maxC = Math.max(maxC, c);
                }
            }
        }

        const pieceW = (maxC - minC + 1) * cellSize;
        const pieceH = (maxR - minR + 1) * cellSize;
        const offX = (canvas.width - pieceW) / 2;
        const offY = (canvas.height - pieceH) / 2;

        for (let r = minR; r <= maxR; r++) {
            for (let c = minC; c <= maxC; c++) {
                if (shapeCells[r * shapeSize + c] !== 0) {
                    const x = offX + (c - minC) * cellSize + 1;
                    const y = offY + (r - minR) * cellSize + 1;
                    const s = cellSize - 2;
                    drawRoundRect(ctx, x, y, s, s, 5, COLORS[shapeColor], STROKE_HIGHLIGHT);
                }
            }
        }
    },

    clearCanvas(canvasId) {
        const entry = canvasContexts[canvasId];
        if (!entry) return;
        const { ctx, canvas } = entry;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
    }
};

function drawRoundRectPath(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
}

function drawRoundRect(ctx, x, y, w, h, r, fillColor, strokeColor) {
    drawRoundRectPath(ctx, x, y, w, h, r);
    ctx.fillStyle = fillColor;
    ctx.fill();
    if (strokeColor) {
        ctx.strokeStyle = strokeColor;
        ctx.lineWidth = 1;
        ctx.stroke();
    }
}

function drawGemDiamond(ctx, cx, cy, rad) {
    ctx.save();
    ctx.globalAlpha = 0.85;
    ctx.fillStyle = "#FFFFFF";
    ctx.beginPath();
    ctx.moveTo(cx, cy - rad);
    ctx.lineTo(cx + rad, cy);
    ctx.lineTo(cx, cy + rad);
    ctx.lineTo(cx - rad, cy);
    ctx.closePath();
    ctx.fill();
    ctx.restore();
}
