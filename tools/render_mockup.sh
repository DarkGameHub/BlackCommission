#!/bin/zsh
# render_mockup.sh <svg-or-html> <out.png> [W] [H]
# 无头 Chrome 渲染 mockup → 精确 WxH PNG。
# 坑1: 本机 Chrome headless 视口 = window 高度 - 87 (含窗口 UI 计入), 故加 87 再顶部裁切。
# 坑2: 直接开 .svg 会 fit-to-window 出白边 → 调用方应传带 <meta charset=utf-8> 的 HTML 包裹。
# 坑3: sips --cropOffset 被无视(居中裁) → 用内嵌 python 顶部对齐裁切。
set -e
SRC="$1"; OUT="$2"; W="${3:-1920}"; H="${4:-1080}"
CHROME="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
TMP="$(mktemp -d)"
"$CHROME" --headless=new --screenshot="$TMP/shot.png" --window-size=$W,$((H+87)) \
  --hide-scrollbars --force-device-scale-factor=1 "file://$SRC" 2>/dev/null
sips -s format bmp "$TMP/shot.png" --out "$TMP/shot.bmp" >/dev/null
python3 - "$TMP/shot.bmp" "$TMP/crop.bmp" $W $H << 'EOF'
import struct, sys
src, dst, W, H = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
d = open(src, 'rb').read()
off = struct.unpack('<I', d[10:14])[0]
w = struct.unpack('<i', d[18:22])[0]
h = struct.unpack('<i', d[22:26])[0]      # 负数 = top-down
row = (w * 3 + 3) // 4 * 4
assert w == W and abs(h) >= H, (w, h)
body = d[off:off + row * H] if h < 0 else d[off + row * (abs(h) - H):off + row * abs(h)]
hdr = bytearray(d[:off])
hdr[22:26] = struct.pack('<i', -H if h < 0 else H)
hdr[2:6] = struct.pack('<I', off + len(body))
struct.pack_into('<I', hdr, 34, len(body))
open(dst, 'wb').write(bytes(hdr) + body)
EOF
sips -s format png "$TMP/crop.bmp" --out "$OUT" >/dev/null
rm -rf "$TMP"
echo "rendered $OUT (${W}x${H})"
