import requests
import wmi
import hashlib
import platform
import uuid

class AuthApp:
    def __init__(self, name: str, ownerid: str, secret: str, version: str = "1.0", api_url: str = "http://hide.mughalxcheat.xyz:19117"):
        self.name = name
        self.ownerid = ownerid
        self.secret = secret
        
        # Ensure full URL with protocol
        if not api_url.startswith(('http://', 'https://')):
            api_url = 'http://' + api_url
        self.api_url = api_url.rstrip('/') + "/api/auth"
        
        self.version = version
        self.user_data = {}  # username, expiry, hwid (list)
        self.init()

    def _request(self, payload: dict):
        try:
            response = requests.post(self.api_url, json=payload, timeout=10)
            response.raise_for_status()
            return response.json()
        except Exception as e:
            return {"success": False, "message": str(e)}

    def init(self):
        payload = {
            "type": "init",
            "appname": self.name,
            "ownerid": self.ownerid,
            "secret": self.secret
        }
        resp = self._request(payload)
        if resp.get("success"):
            self.version = resp.get("version", self.version)
        return resp

    def login(self, username: str, password: str):
        hwid = self.get_hwid()
        payload = {
            "type": "login",
            "appname": self.name,
            "ownerid": self.ownerid,
            "secret": self.secret,
            "username": username,
            "password": password,
            "hwid": hwid
        }
        resp = self._request(payload)
        if resp.get("success"):
            self.user_data = resp.get("user", {})
        return resp

    def license(self, key: str):
        hwid = self.get_hwid()
        payload = {
            "type": "license",
            "appname": self.name,
            "ownerid": self.ownerid,
            "secret": self.secret,
            "key": key,
            "hwid": hwid
        }
        return self._request(payload)

    def get_hwid(self):
        """
        Real system HWID generation (consistent on same PC)
        Uses WMI for Processor ID + fallback to machine info
        """
        try:
            c = wmi.WMI()
            # Primary: Processor ID
            for cpu in c.Win32_Processor():
                return cpu.ProcessorId.strip()
            # Fallback: Disk Serial
            for disk in c.Win32_DiskDrive():
                return disk.SerialNumber.strip()
        except Exception as e:
            print(f"HWID fallback used: {e}")
        
        # Ultimate fallback: machine + user + platform hash
        info = platform.node() + platform.system() + platform.processor()
        return hashlib.sha256(info.encode()).hexdigest()

    def get_user_info(self):
        return self.user_data