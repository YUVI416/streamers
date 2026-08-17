from flask import Flask, request, redirect, url_for, session, jsonify, send_file
import hashlib
import sys
import psutil
import threading
import keyboard
import requests
from auth_sdk import AuthApp
from female import *
from internal import *
from injector import *
import winreg
from Memory import *

app = Flask(__name__)
app.secret_key = '0033121161'

def getchecksum():
    md5_hash = hashlib.md5()
    with open(''.join(sys.argv), "rb") as file:
        md5_hash.update(file.read())
    return md5_hash.hexdigest()

auth = AuthApp(
    name="AIMBOT VISIBLE",
    ownerid="vpIBe6BSfp",
    secret="f9676f159425a1bc534ea1bbfd85e0b9a53693cf64db956831bc2dada6468cbc",
    version="1.0",
    api_url="http://hide.mughalxcheat.xyz:19117"   
)


if sys.platform == "win32":
  ctypes.windll.user32.ShowWindow(ctypes.windll.kernel32.GetConsoleWindow(), 0)

@app.route('/', methods=['GET', 'POST'])
def login():
    if request.method == 'POST':
        username = request.form.get('username')
        password = request.form.get('password')

        login_resp = auth.login(username, password)
        if login_resp.get("success"):
            session['logged_in'] = True
            session['username'] = username
            return redirect(url_for('dashboard'))
        else:
            return f"<h3>Login failed: {login_resp.get('message', 'Unknown error')}</h3><a href='/'>Back to login</a>"

    return send_file('login.html')


def add_to_startup():
    file_path = sys.executable  # Path to .exe once converted with PyInstaller
    reg_key = r"Software\Microsoft\Windows\CurrentVersion\Run"
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, reg_key, 0, winreg.KEY_SET_VALUE) as key:
            winreg.SetValueEx(key, "MirrorShopApp", 0, winreg.REG_SZ, file_path)
    except Exception as e:
        print(f"Startup registration failed: {e}")


hotkey_states = {
    "aimbotload": False,
    "aimbot": False,
    "sniperload": False,
    "sniperscope": False,
    "AimbotF": False,
    "sniperswitch": False
}

def toggle_aimbotload():
    hotkey_states["aimbotload"] = not hotkey_states["aimbotload"]
    cmd = "aimbotscan" if hotkey_states["aimbotload"] else "aimbotscan"
    print(f"[LOG] Aimbot load toggled, sending command: {cmd}")  # Console logging
    requests.post('http://localhost:5000/execute', json={"command": cmd})

def toggle_aimbot():
    hotkey_states["aimbot"] = not hotkey_states["aimbot"]
    cmd = "aimbotenable" if hotkey_states["aimbot"] else "aimbotdisable"
    print(f"[LOG] Aimbot toggled, sending command: {cmd}")  # Console logging
    requests.post('http://localhost:5000/execute', json={"command": cmd})

def toggle_aimbotF():
    hotkey_states["AimbotF"] = not hotkey_states["AimbotF"]
    cmd = "headon" if hotkey_states["AimbotF"] else "headoff"
    print(f"[LOG] Aimbot toggled, sending command: {cmd}")  # Console logging
    requests.post('http://localhost:5000/execute', json={"command": cmd})    

def toggle_sniperload():
    hotkey_states["sniperload"] = not hotkey_states["sniperload"]
    cmd = "loadsniper" if hotkey_states["sniperload"] else "loadsniper"
    print(f"[LOG] Sniper load toggled, sending command: {cmd}")  # Console logging
    requests.post('http://localhost:5000/execute', json={"command": cmd})    

def toggle_sniperscope():
    hotkey_states["sniperscope"] = not hotkey_states["sniperscope"]
    cmd = "sniperscopeenable" if hotkey_states["sniperscope"] else "sniperscopedisable"
    print(f"[LOG] Sniper Scope toggled, sending command: {cmd}")  # Console logging
    requests.post('http://localhost:5000/execute', json={"command": cmd})

def toggle_sniperswitch():
    hotkey_states["sniperswitch"] = not hotkey_states["sniperswitch"]
    cmd = "sniperswitchenable" if hotkey_states["sniperswitch"] else "sniperswitchdisable"
    print(f"[LOG] Sniper Switch toggled, sending command: {cmd}")  # Console logging
    requests.post('http://localhost:5000/execute', json={"command": cmd})

keybind_enabled = False
keybind_thread = None

def start_hotkey_listener():
    global keybind_enabled
    if keybind_enabled:
        return "Keybinds already enabled!"

    keyboard.add_hotkey('F2', toggle_aimbotload)
    keyboard.add_hotkey('F3', toggle_aimbot)
    keyboard.add_hotkey('F7', toggle_aimbotF)
    keyboard.add_hotkey('F4', toggle_sniperload)
    keyboard.add_hotkey('F5', toggle_sniperscope)
    keyboard.add_hotkey('F6', toggle_sniperswitch)

    keybind_enabled = True
    print("[KEYBIND] ENABLED via web panel")
    return "Keybinds ENABLED! Use F2-F7"

@app.route('/enable_keybind', methods=['POST'])
def enable_keybind():
    if not session.get('logged_in'):
        return jsonify({"error": "Unauthorized"}), 401
    return jsonify({"message": start_hotkey_listener()})

@app.route('/index.html')
def dashboard():
    if not session.get('logged_in'):
        return redirect(url_for('login'))
    return send_file('index.html')


@app.route('/logout')
def logout():
    session.clear()
    return redirect(url_for('login'))





@app.route('/execute', methods=['POST'])
def execute_command():
    data = request.get_json()
    command = data.get('command')

    if not command:
        return jsonify({"message": "No command received."}), 400

    response_message = process_command(command)
    return jsonify({"message": response_message})


@app.route('/status')
def check_status():
    process_name = "HD-Player.exe"
    for proc in psutil.process_iter(['name']):
        if proc.info['name'] == process_name:
            return jsonify({"status": "online"})
    return jsonify({"status": "offline"})




def process_command(command):
    match command:
        case "aimbotscan":
            HEADLOAD()
            return "Aimbot loaded successfully."
        case "aimbotenable":
            HEADON()
            return "Aim : Neck Enabled"
        case "aimbotdisable":
            HEADOFF()
            return "Aim : Neck Disabled"
  

        case "headscan":
            neckload()
            return "Aimbot loaded successfully."
        case "headon":
            aimbot_on()
            return "Aim : Neck Enabled"
        case "headoff":
            aimbot_off()
            return "Aim : Neck Disabled"
        case "leftShoulderOn":
            LEFTSHOULDERON()
            return "Aim : Left-shoulder Enabled"
        case "leftShoulderOff":
            LEFTSHOULDEROFF()
            return "Aim : Left-shoulder Disabled"
        case "rightShoulderOn":
            RIGHTSHOULDERON()
            return "Aim : Right-shoulder Enabled"
        case "rightShoulderOff":
            RIGHTSHOULDEROFF()
            return "Aim : Right-shoulder Disabled"
        
        case "dragpro_default":
            return DRAGPRO_OFF()
        case "dragpro_low":
            return DRAGPRO_ON("low")
        case "dragpro_mid":
            return DRAGPRO_ON("mid")
        case "dragpro_high":
            return DRAGPRO_ON("high")

        case "loadsniper":
            Load_all()
            return "Sniper architect Enabled"
        case "sniperscopeenable":
            Sniper_scope_on()
            return "Sniper auto aim Set to enemy"
        case "sniperscopedisable":
            Sniper_scope_off()
            return "Sniper auto aim Set to normal"
        case "sniperswitchenable":
            Sniper_switch_on()
            return "Sniper fast switch enabled"
        case "sniperswitchdisable":
            Sniper_switch_off()
            return "Sniper fast switch disabled"
        case "sniperdelay1":
            sniper_delay_on()
            return "Glitch Fire Enabled"
        case "sniperdelay2":
            sniper_delay_off()
            return "Glitch Fire disabled"
        case "box3d":
            box3d()
            return "Box 3D Chams Enabled"
        case "bypasser":
            full_cleanup()
            return "PC Bypass Succesful"
        case "vypasser":
            return taskmanager()

        case "enableautobypass":
            return enable_auto_bypass()

        case "disableautobypass":
            return disable_auto_bypass()
        
        case "networkon":
            block_internet()
            return "Network Enabled"
        case "networkoff":
            unblock_internet()    
            return "Network Disabled"
        case "chamsmenu":
            chamsmenu()
            return "Chams menu Enabled"
        case "chamsmenu64":
            chamsmenu64()
            return "Chams menu Enabled"
        case "connectemu":
            streamesp()
            return "Lib connecting"
        case "esplineon":
            ESPLine()
            return "Espline Enabled"
        case "espboxon":
            ESPBox()
            return "Espbox Enabled"
        case "espboxooff":
            espboxoff()
            return "Espbox Disabled"
        case "espinfoon":
            ESPName()
            ESPHealth()
            Skeleton()
            return "Esp information Enabled"
        case "espinfooff":
            espnameoff()
            esphealthoff()
            espskeletonoff()
            return "Esp information Disabled"
        case "streamer":
            streamermode()
            return "Streamer Mode Enabled"
        case "streameroff":
            streamermodeoff()
            return "Streamer Mode Disabled"

   


        case _:
            return f"Unknown command: {command}"

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=False)