import os

pathex = [r"E:\ALL SOURCE\QUANTUM STREAMER PYTHON"]

a = Analysis(
    ['app.py'],
    pathex=pathex,
    binaries=[],
    datas=[
        ('index.html', '.'),            # Include index.html from root
        ('login.html', '.'),            # Include login.html from root
        ('AotBst.dll', '.'),
        ('cimgui.dll', '.'),
        ('Client.dll', '.')
    ],
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)

pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='Svchost',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=r"C:\Program Files\Notepad++",
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
