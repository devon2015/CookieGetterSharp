using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Hal.CookieGetterSharp {
    /// <summary>
    /// 小規模関数群
    /// </summary>
    public class Utility {
        /// <summary>
        /// Unix時間をDateTimeに変換する
        /// </summary>
        /// <param name="UnixTime"></param>
        /// <returns></returns>
        public static DateTime UnixTimeToDateTime(int UnixTime) {
            return new DateTime(1970, 1, 1, 9, 0, 0).AddSeconds(UnixTime);
        }

        /// <summary>
        /// DateTimeをUnix時間に変換する
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static int DateTimeToUnixTime(DateTime time) {
            TimeSpan t = time.Subtract(new DateTime(1970, 1, 1, 9, 0, 0));
            return (int)t.TotalSeconds;
        }

        /// <summary>
        /// %APPDATA%などを実際のパスに変換する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ReplacePathSymbols(string path) {
            path = path.Replace("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            path = path.Replace("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            path = path.Replace("%COOKIES%", Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            return path;
        }

        /// <summary>
        /// 必要があればuriの最後に/をつける
        /// Pathの指定がある場合、uriの最後に/があるかないかで取得できない場合があるので
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri AddSrashLast(Uri uri) {
            string o = uri.Segments[uri.Segments.Length - 1];
            string no = uri.OriginalString;//.Replace("http://", "http://o.");
            if(!o.Contains(".") && o[o.Length - 1] != '/') {
                no += "/";
            }
            return new Uri(no);
        }

        /// <summary>
        /// クッキーコンテナにクッキーを追加する
        /// domainが.hal.fscs.jpなどだと http://hal.fscs.jp でクッキーが有効にならないので.ありとなし両方指定する
        /// </summary>
        /// <param name="container"></param>
        /// <param name="cookie"></param>
        public static void AddCookieToContainer(System.Net.CookieContainer container, System.Net.Cookie cookie) {

            if(container == null) {
                throw new ArgumentNullException("container");
            }

            if(cookie == null) {
                throw new ArgumentNullException("cookie");
            }

            container.Add(cookie);
            if(cookie.Domain.StartsWith(".")) {
                container.Add(new System.Net.Cookie() {
                    Comment = cookie.Comment,
                    CommentUri = cookie.CommentUri,
                    Discard = cookie.Discard,
                    Domain = cookie.Domain.Substring(1),
                    Expired = cookie.Expired,
                    Expires = cookie.Expires,
                    HttpOnly = cookie.HttpOnly,
                    Name = cookie.Name,
                    Path = cookie.Path,
                    Port = cookie.Port,
                    Secure = cookie.Secure,
                    Value = cookie.Value,
                    Version = cookie.Version,
                });
            }

        }

        /// <summary>
        /// 実行プログラムフォルダ内に一時ファイル名の取得
        /// </summary>
        /// <returns></returns>
        public static string GetTempFilePath() {
            string uniqueString = Guid.NewGuid().ToString() + (System.Environment.TickCount & int.MaxValue).ToString();
            //    string applicationPath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            string roamingPath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CookieGetterSharp");
            if(!System.IO.Directory.Exists(roamingPath)) System.IO.Directory.CreateDirectory(roamingPath);
            string uniquePath = System.IO.Path.Combine(roamingPath, md5(uniqueString));
            return uniquePath;
        }

        /// <summary>
        /// MD5ハッシュを生成
        /// </summary>
        /// <param name="textData"></param>
        /// <returns></returns>
        public static string md5(string textData) {
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(textData);
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bs = md5.ComputeHash(byteData);
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            foreach(byte b in bs) {
                result.Append(b.ToString("x2"));
            }

            return result.ToString();
        }

        /// <summary>
        /// url上のページを取得する
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookies"></param>
        /// <param name="defaultTimeout"></param>
        /// <returns></returns>
        static internal string GetResponseText(string url, CookieContainer cookies, int defaultTimeout) {
            HttpWebResponse webRes = null;
            StreamReader sr = null;

            try {
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);

                webReq.Timeout = defaultTimeout;
                webReq.CookieContainer = cookies;
                webReq.UserAgent = "CookieGetterSharp (+http://com.nicovideo.jp/community/co235502)";

                try {
                    webRes = (HttpWebResponse)webReq.GetResponse();
                }
                catch(WebException ex) {
                    webRes = ex.Response as HttpWebResponse;

                }

                if(webRes == null) {
                    return null;
                }

                sr = new StreamReader(webRes.GetResponseStream(), System.Text.Encoding.UTF8);
                return sr.ReadToEnd();

            }
            finally {
                if(webRes != null)
                    webRes.Close();
                if(sr != null)
                    sr.Close();
            }
        }

        /// <summary>
        /// シリアライズします
        /// </summary>
        /// <param name="path"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static bool Serialize(string path, object graph) {

            try {

                System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                using(Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    formatter.Serialize(stream, graph);
                    stream.Close();
                }

                return true;
            }
            catch {
            }

            return false;
        }

        /// <summary>
        /// デシリアライズします
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static object Deserialize(string path) {

            if(File.Exists(path)) {
                try {
                    System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    using(Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        return formatter.Deserialize(stream);
                    }

                }
                catch {
                }

            }

            return null;
        }

    }
    internal static class win32api {
        /// <summary>
        /// 保護モードIEからCookieを取得する
        /// </summary>
        /// <param name="cookiesText">取得したCookieの代入先</param>
        /// <param name="targetUrl">Cookieを送りたいページのURL</param>
        /// <param name="valueKey">読み出したいCookieのキー値</param>
        /// <returns>引数targetUrlに対して使えるCookieヘッダー値</returns>
        public static int GetCookiesFromProtectedModeIE(out string cookiesText, Uri targetUrl, string valueKey = null) {
            var cookieSize = 4096;
            var lpszCookieData = new StringBuilder(cookieSize);
            var dwSizeP = new IntPtr(cookieSize);

            do {
                var hResult = IEGetProtectedModeCookie(
                    targetUrl.OriginalString, valueKey, lpszCookieData, ref cookieSize, INTERNET_COOKIE_HTTPONLY);
                switch((uint)hResult) {
                    case 0x8007007A://バッファー不足
                        cookieSize = cookieSize + 256;
                        lpszCookieData.Capacity = cookieSize;
                        continue;
                    case 0x00000000://S_OK
                    case 0x80070103://データ無し
                    case 0x80070057://E_INVALIDARG: IEが非保護モードだと出てきたりする
                        cookiesText = lpszCookieData.ToString();
                        return hResult;
                    default:
                        cookiesText = null;
                        return hResult;
                }
            }
            while(true);
        }

        /// <summary>
        /// 従来モードIEからCookieを取得する
        /// </summary>
        /// <param name="cookiesText">取得したCookieの代入先</param>
        /// <param name="targetUrl">Cookieを送りたいページのURL</param>
        /// <param name="valueKey">読み出したいCookieのキー値</param>
        /// <returns>引数targetUrlに対して使えるCookieヘッダー値</returns>
        public static int GetCookiesFromIE(out string cookiesText, Uri targetUrl, string valueKey = null) {
            var cookieSize = 4096;
            var lpszCookieData = new StringBuilder(cookieSize);
            var dwSizeP = new IntPtr(cookieSize);

            //クッキー文字列取得
            do {
                bool bResult;
                if(bResult = win32api.InternetGetCookieEx(targetUrl.OriginalString, valueKey, lpszCookieData, ref cookieSize, win32api.INTERNET_COOKIE_HTTPONLY, IntPtr.Zero)) {
                    cookiesText = lpszCookieData.ToString();
                    return 0x00000000;
                }
                else {
                    var errorNo = Marshal.GetHRForLastWin32Error();
                    switch((uint)errorNo) {
                        case 0x00000000:
                        case 0x80070103:
                            cookiesText = lpszCookieData.ToString();
                            return errorNo;
                        case 0x8007007A:
                            //バッファーサイズ拡張。無限ループが怖いので一応必ずサイズが増えるようにしておく
                            cookieSize += 512;
                            lpszCookieData.Capacity = cookieSize;
                            continue;
                        default:
                            cookiesText = null;
                            return errorNo;
                    }
                }
            }
            while(true);
        }

        /// <summary>
        /// IEのバージョンを取得する
        /// </summary>
        /// <returns>ex:11.0.9600.17239</returns>
        public static Version GetIEVersion() {
            try {
                using(var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Internet Explorer"))
                    return Version.Parse((string)key.GetValue("svcVersion") ?? (string)key.GetValue("Version"));
            }
            catch { return null; }
        }

        public const uint INTERNET_COOKIE_THIRD_PARTY = 0x00000010;
        public const uint INTERNET_COOKIE_HTTPONLY = 0x00002000;
        public const uint INTERNET_FLAG_RESTRICTED_ZONE = 0x00020000;

        [DllImport("Wininet", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool InternetGetCookieEx(string lpszURL, string lpszCookieName, StringBuilder lpszCookieData, ref int lpdwSize, uint dwFlags, IntPtr lpReserved);

        [DllImport("Wininet", CharSet = CharSet.Unicode)]
        static extern bool InternetGetCookie(string lpszURL, string lpszCookieName, StringBuilder lpszCookieData, ref uint lpdwSize);
        //, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, ExactSpelling=true

        /*
         * HRESULT IEGetProtectedModeCookie(
         *   _In_     LPCWSTR lpszURL,
         *   _In_     LPCWSTR lpszCookieName,
         *   _Inout_  LPWSTR pszCookieData,
         *   _Inout_  DWORD *pcchCookieData,
         *   _In_     DWORD dwFlags
         * );
         */
        [DllImport("ieframe.dll", CharSet = CharSet.Unicode)]
        static extern int IEGetProtectedModeCookie(string lpszURL, string lpszCookieName, StringBuilder pszCookieData, ref int pcchCookieData, uint dwFlags);



        /*
         * typedef struct _CRYPTOAPI_BLOB {
         *   DWORD cbData;
         *   BYTE  *pbData;
         * } CRYPT_INTEGER_BLOB, *PCRYPT_INTEGER_BLOB, CRYPT_UINT_BLOB, *PCRYPT_UINT_BLOB, CRYPT_OBJID_BLOB, *PCRYPT_OBJID_BLOB, CERT_NAME_BLOB, CERT_RDN_VALUE_BLOB, *PCERT_NAME_BLOB, *PCERT_RDN_VALUE_BLOB, CERT_BLOB, *PCERT_BLOB, CRL_BLOB, *PCRL_BLOB, DATA_BLOB, *PDATA_BLOB, CRYPT_DATA_BLOB, *PCRYPT_DATA_BLOB, CRYPT_HASH_BLOB, *PCRYPT_HASH_BLOB, CRYPT_DIGEST_BLOB, *PCRYPT_DIGEST_BLOB, CRYPT_DER_BLOB, PCRYPT_DER_BLOB, CRYPT_ATTR_BLOB, *PCRYPT_ATTR_BLOB;
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct DATA_BLOB {
            public uint cbData;
            public IntPtr pbData;
        }

        /*
         * BOOL WINAPI CryptUnprotectData(
         *   _In_        DATA_BLOB *pDataIn,
         *   _Out_opt_   LPWSTR *ppszDataDescr,
         *   _In_opt_    DATA_BLOB *pOptionalEntropy,
         *   _Reserved_  PVOID pvReserved,
         *   _In_opt_    CRYPTPROTECT_PROMPTSTRUCT *pPromptStruct,
         *   _In_        DWORD dwFlags,
         *   _Out_       DATA_BLOB *pDataOut
         * );
         */
        [DllImport("Crypt32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        public static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, string ppszDataDescr, ref  DATA_BLOB pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, uint dwFlags, [In, Out]ref DATA_BLOB pDataOut);

        /*
         * HLOCAL WINAPI LocalFree(
         *   _In_  HLOCAL hMem
         * );
         */
        [DllImport("Kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr hMem);
    }
}