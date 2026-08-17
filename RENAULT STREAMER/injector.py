import os
import shutil
import pymem
import time

def inject_dll_from_path(process, dll_path):
    try:
       
        pymem.process.inject_dll_from_path(process.process_handle, dll_path)
        print(f"{dll_path} Injected Successfully!")
    except Exception as e:
        print(f"Failed to inject {dll_path}: {e}")

def streamesp():
    process_name = "HD-Player.exe"

    try:
        
        process = pymem.Pymem(process_name)

       
        temp_dll_path_1 = os.path.join(os.path.abspath(os.path.dirname(__file__)), 'cimgui.dll')
        temp_dll_path_2 = os.path.join(os.path.abspath(os.path.dirname(__file__)), 'AotBst.dll')

      
        temp_dll_path_0 = os.path.join(os.path.abspath(os.path.dirname(__file__)), 'Client.dll')
        if os.path.exists(temp_dll_path_0):
            client_dll_temp_path = os.path.join("C:\\Windows\\Temp", "Client.dll")
            try:
                shutil.copy(temp_dll_path_0, client_dll_temp_path)  
                print(f"Copied Client.dll to {client_dll_temp_path}")
            except OSError as e:
                print(f"Error copying {temp_dll_path_0} to {client_dll_temp_path}: {e}")
                return

       
        inject_dll_from_path(process, temp_dll_path_1)
        time.sleep(1)  
        inject_dll_from_path(process, temp_dll_path_2)
        print("Injection completed successfully.")

    except pymem.exception.ProcessNotFound:
        print("Emulator not found.")
    except Exception as e:
        print(f"Error: {e}")