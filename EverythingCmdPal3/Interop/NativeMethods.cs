using EverythingCmdPal3.Properties;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace EverythingCmdPal3.Interop
{
    internal sealed class NativeMethods
    {
        #region FlagsEnum
        [Flags]
        public enum Property
        {
            NAME = 0,
            PATH = 1,
            SIZE = 2,
            EXTENSION = 3,
            TYPE = 4,
            DATE_MODIFIED = 5,
            DATE_CREATED = 6,
            DATE_ACCESSED = 7,
            ATTRIBUTES = 8,
            DATE_RECENTLY_CHANGED = 9,
            RUN_COUNT = 10,
            DATE_RUN = 11,
            FILE_LIST_FILENAME = 12,
            WIDTH = 13,
            HEIGHT = 14,
            DIMENSIONS = 15,
            ASPECT_RATIO = 16,
            BIT_DEPTH = 17,
            LENGTH = 18,
            AUDIO_SAMPLE_RATE = 19,
            AUDIO_CHANNELS = 20,
            AUDIO_BITS_PER_SAMPLE = 21,
            AUDIO_BIT_RATE = 22,
            AUDIO_FORMAT = 23,
            FILE_SIGNATURE = 24,
            TITLE = 25,
            ARTIST = 26,
            ALBUM = 27,
            YEAR = 28,
            COMMENT = 29,
            TRACK = 30,
            GENRE = 31,
            FRAME_RATE = 32,
            VIDEO_BIT_RATE = 33,
            VIDEO_FORMAT = 34,
            RATING = 35,
            TAGS = 36,
            MD5 = 37,
            SHA1 = 38,
            SHA256 = 39,
            CRC32 = 40,
            SIZE_ON_DISK = 41,
            DESCRIPTION = 42,
            VERSION = 43,
            PRODUCT_NAME = 44,
            PRODUCT_VERSION = 45,
            COMPANY = 46,
            KIND = 47,
            FILE_NAME_LENGTH = 48,
            FULL_PATH_LENGTH = 49,
            SUBJECT = 50,
            AUTHORS = 51,
            DATE_TAKEN = 52,
            SOFTWARE = 53,
            DATE_ACQUIRED = 54,
            COPYRIGHT = 55,
            IMAGE_ID = 56,
            HORIZONTAL_RESOLUTION = 57,
            VERTICAL_RESOLUTION = 58,
            COMPRESSION = 59,
            RESOLUTION_UNIT = 60,
            COLOR_REPRESENTATION = 61,
            COMPRESSED_BITS_PER_PIXEL = 62,
            CAMERA_MAKER = 63,
            CAMERA_MODEL = 64,
            F_STOP = 65,
            EXPOSURE_TIME = 66,
            ISO_SPEED = 67,
            EXPOSURE_BIAS = 68,
            FOCAL_LENGTH = 69,
            MAX_APERTURE = 70,
            METERING_MODE = 71,
            SUBJECT_DISTANCE = 72,
            FLASH_MODE = 73,
            FLASH_ENERGY = 74,
            _35MM_FOCAL_LENGTH = 75,
            LENS_MAKER = 76,
            LENS_MODEL = 77,
            FLASH_MAKER = 78,
            FLASH_MODEL = 79,
            CAMERA_SERIAL_NUMBER = 80,
            CONTRAST = 81,
            BRIGHTNESS = 82,
            LIGHT_SOURCE = 83,
            EXPOSURE_PROGRAM = 84,
            SATURATION = 85,
            SHARPNESS = 86,
            WHITE_BALANCE = 87,
            PHOTOMETRIC_INTERPRETATION = 88,
            DIGITAL_ZOOM = 89,
            EXIF_VERSION = 90,
            LATITUDE = 91,
            LONGITUDE = 92,
            ALTITUDE = 93,
            SUBTITLE = 94,
            TOTAL_BIT_RATE = 95,
            DIRECTORS = 96,
            PRODUCERS = 97,
            WRITERS = 98,
            PUBLISHER = 99,
            CONTENT_DISTRIBUTOR = 100,
            DATE_ENCODED = 101,
            ENCODED_BY = 102,
            AUTHOR_URL = 103,
            PROMOTION_URL = 104,
            OFFLINE_AVAILABILITY = 105,
            OFFLINE_STATUS = 106,
            SHARED_WITH = 107,
            OWNER = 108,
            COMPUTER = 109,
            ALBUM_ARTIST = 110,
            PARENTAL_RATING_REASON = 111,
            COMPOSER = 112,
            CONDUCTOR = 113,
            CONTENT_GROUP_DESCRIPTION = 114,
            MOOD = 115,
            PART_OF_SET = 116,
            INITIAL_KEY = 117,
            BEATS_PER_MINUTE = 118,
            PROTECTED = 119,
            PART_OF_A_COMPILATION = 120,
            PARENTAL_RATING = 121,
            PERIOD = 122,
            PEOPLE = 123,
            CATEGORY = 124,
            CONTENT_STATUS = 125,
            DOCUMENT_CONTENT_TYPE = 126,
            PAGE_COUNT = 127,
            WORD_COUNT = 128,
            CHARACTER_COUNT = 129,
            LINE_COUNT = 130,
            PARAGRAPH_COUNT = 131,
            TEMPLATE = 132,
            SCALE = 133,
            LINKS_DIRTY = 134,
            LANGUAGE = 135,
            LAST_AUTHOR = 136,
            REVISION_NUMBER = 137,
            VERSION_NUMBER = 138,
            MANAGER = 139,
            DATE_CONTENT_CREATED = 140,
            DATE_SAVED = 141,
            DATE_PRINTED = 142,
            TOTAL_EDITING_TIME = 143,
            ORIGINAL_FILE_NAME = 144,
            DATE_RELEASED = 145,
            SLIDE_COUNT = 146,
            NOTE_COUNT = 147,
            HIDDEN_SLIDE_COUNT = 148,
            PRESENTATION_FORMAT = 149,
            TRADEMARKS = 150,
            DISPLAY_NAME = 151,
            FILE_NAME_LENGTH_IN_UTF8_BYTES = 152,
            FULL_PATH_LENGTH_IN_UTF8_BYTES = 153,
            CHILD_COUNT = 154,
            CHILD_FOLDER_COUNT = 155,
            CHILD_FILE_COUNT = 156,
            CHILD_COUNT_FROM_DISK = 157,
            CHILD_FOLDER_COUNT_FROM_DISK = 158,
            CHILD_FILE_COUNT_FROM_DISK = 159,
            FOLDER_DEPTH = 160,
            TOTAL_SIZE = 161,
            TOTAL_SIZE_ON_DISK = 162,
            DATE_CHANGED = 163,
            HARD_LINK_COUNT = 164,
            DELETE_PENDING = 165,
            IS_DIRECTORY = 166,
            ALTERNATE_DATA_STREAM_COUNT = 167,
            ALTERNATE_DATA_STREAM_NAMES = 168,
            TOTAL_ALTERNATE_DATA_STREAM_SIZE = 169,
            TOTAL_ALTERNATE_DATA_STREAM_SIZE_ON_DISK = 170,
            COMPRESSED_SIZE = 171,
            COMPRESSION_FORMAT = 172,
            COMPRESSION_UNIT_SHIFT = 173,
            COMPRESSION_CHUNK_SHIFT = 174,
            COMPRESSION_CLUSTER_SHIFT = 175,
            COMPRESSION_RATIO = 176,
            REPARSE_TAG = 177,
            REMOTE_PROTOCOL = 178,
            REMOTE_PROTOCOL_VERSION = 179,
            REMOTE_PROTOCOL_FLAGS = 180,
            LOGICAL_BYTES_PER_SECTOR = 181,
            PHYSICAL_BYTES_PER_SECTOR_FOR_ATOMICITY = 182,
            PHYSICAL_BYTES_PER_SECTOR_FOR_PERFORMANCE = 183,
            EFFECTIVE_PHYSICAL_BYTES_PER_SECTOR_FOR_ATOMICITY = 184,
            FILE_STORAGE_INFO_FLAGS = 185,
            BYTE_OFFSET_FOR_SECTOR_ALIGNMENT = 186,
            BYTE_OFFSET_FOR_PARTITION_ALIGNMENT = 187,
            ALIGNMENT_REQUIREMENT = 188,
            VOLUME_SERIAL_NUMBER = 189,
            FILE_ID = 190,
            FRAME_COUNT = 191,
            CLUSTER_SIZE = 192,
            SECTOR_SIZE = 193,
            AVAILABLE_FREE_DISK_SIZE = 194,
            FREE_DISK_SIZE = 195,
            TOTAL_DISK_SIZE = 196,
            UNUSED197 = 197,
            MAXIMUM_COMPONENT_LENGTH = 198,
            FILE_SYSTEM_FLAGS = 199,
            FILE_SYSTEM = 200,
            ORIENTATION = 201,
            END_OF_FILE = 202,
            SHORT_NAME = 203,
            SHORT_FULL_PATH = 204,
            ENCRYPTION_STATUS = 205,
            HARD_LINK_FILE_NAMES = 206,
            INDEX_TYPE = 207,
            DRIVE_TYPE = 208,
            BINARY_TYPE = 209,
            REGEX_MATCH_0 = 210,
            REGEX_MATCH_1 = 211,
            REGEX_MATCH_2 = 212,
            REGEX_MATCH_3 = 213,
            REGEX_MATCH_4 = 214,
            REGEX_MATCH_5 = 215,
            REGEX_MATCH_6 = 216,
            REGEX_MATCH_7 = 217,
            REGEX_MATCH_8 = 218,
            REGEX_MATCH_9 = 219,
            SIBLING_COUNT = 220,
            SIBLING_FOLDER_COUNT = 221,
            SIBLING_FILE_COUNT = 222,
            INDEX_NUMBER = 223,
            SHORTCUT_TARGET = 224,
            OUT_OF_DATE = 225,
            INCUR_SEEK_PENALTY = 226,
            PLAIN_TEXT_LINE_COUNT = 227,
            APERTURE = 228,
            MAKER_NOTE = 229,
            RELATED_SOUND_FILE = 230,
            SHUTTER_SPEED = 231,
            TRANSCODED_FOR_SYNC = 232,
            CASE_SENSITIVE_DIR = 233,
            DATE_INDEXED = 234,
            NAME_FREQUENCY = 235,
            SIZE_FREQUENCY = 236,
            EXTENSION_FREQUENCY = 237,
            REGEX_MATCHES = 238,
            URL = 239,
            FULL_PATH = 240,
            PARENT_FILE_ID = 241,
            SHA512 = 242,
            SHA384 = 243,
            CRC64 = 244,
            FIRST_BYTE = 245,
            FIRST_2_BYTES = 246,
            FIRST_4_BYTES = 247,
            FIRST_8_BYTES = 248,
            FIRST_16_BYTES = 249,
            FIRST_32_BYTES = 250,
            FIRST_64_BYTES = 251,
            FIRST_128_BYTES = 252,
            LAST_BYTE = 253,
            LAST_2_BYTES = 254,
            LAST_4_BYTES = 255,
            LAST_8_BYTES = 256,
            LAST_16_BYTES = 257,
            LAST_32_BYTES = 258,
            LAST_64_BYTES = 259,
            LAST_128_BYTES = 260,
            BYTE_ORDER_MARK = 261,
            VOLUME_LABEL = 262,
            FILE_LIST_FULL_PATH = 263,
            DISPLAY_FULL_PATH = 264,
            PARSE_NAME = 265,
            PARSE_FULL_PATH = 266,
            STEM = 267,
            SHELL_ATTRIBUTES = 268,
            IS_FOLDER = 269,
            VALUTF8 = 270,
            STEM_LENGTH = 271,
            EXTENSION_LENGTH = 272,
            PATH_PART_LENGTH = 273,
            TIME_MODIFIED = 274,
            TIME_CREATED = 275,
            TIME_ACCESSED = 276,
            DAY_MODIFIED = 277,
            DAY_CREATED = 278,
            DAY_ACCESSED = 279,
            PARENT_NAME = 280,
            REPARSE_TARGET = 281,
            DESCENDANT_COUNT = 282,
            DESCENDANT_FOLDER_COUNT = 283,
            DESCENDANT_FILE_COUNT = 284,
            FROM = 285,
            TO = 286,
            DATE_RECEIVED = 287,
            DATE_SENT = 288,
            CONTAINER_FILENAMES = 289,
            CONTAINER_FILE_COUNT = 290,
            CUSTOM_PROPERTY_0 = 291,
            CUSTOM_PROPERTY_1 = 292,
            CUSTOM_PROPERTY_2 = 293,
            CUSTOM_PROPERTY_3 = 294,
            CUSTOM_PROPERTY_4 = 295,
            CUSTOM_PROPERTY_5 = 296,
            CUSTOM_PROPERTY_6 = 297,
            CUSTOM_PROPERTY_7 = 298,
            CUSTOM_PROPERTY_8 = 299,
            CUSTOM_PROPERTY_9 = 300,
            ALLOCATION_SIZE = 301,
            SFV_CRC32 = 302,
            MD5SUM_MD5 = 303,
            SHA1SUM_SHA1 = 304,
            SHA256SUM_SHA256 = 305,
            SFV_PASS = 306,
            MD5SUM_PASS = 307,
            SHA1SUM_PASS = 308,
            SHA256SUM_PASS = 309,
            ALTERNATE_DATA_STREAM_ANSI = 310,
            ALTERNATE_DATA_STREAM_UTF8 = 311,
            ALTERNATE_DATA_STREAM_UTF16LE = 312,
            ALTERNATE_DATA_STREAM_UTF16BE = 313,
            ALTERNATE_DATA_STREAM_TEXT_PLAIN = 314,
            ALTERNATE_DATA_STREAM_HEX = 315,
            PERCEIVED_TYPE = 316,
            CONTENT_TYPE = 317,
            OPENED_BY = 318,
            TARGET_MACHINE = 319,
            SHA512SUM_SHA512 = 320,
            SHA512SUM_PASS = 321,
            PARENT_PATH = 322,
            FIRST_256_BYTES = 323,
            FIRST_512_BYTES = 324,
            LAST_256_BYTES = 325,
            LAST_512_BYTES = 326,
            INDEX_ONLINE = 327,
            COLUMN_0 = 328,
            COLUMN_1 = 329,
            COLUMN_2 = 330,
            COLUMN_3 = 331,
            COLUMN_4 = 332,
            COLUMN_5 = 333,
            COLUMN_6 = 334,
            COLUMN_7 = 335,
            COLUMN_8 = 336,
            COLUMN_9 = 337,
            COLUMN_A = 338,
            COLUMN_B = 339,
            COLUMN_C = 340,
            COLUMN_D = 341,
            COLUMN_E = 342,
            COLUMN_F = 343,
            ZONE_ID = 344,
            REFERRER_URL = 345,
            HOST_URL = 346,
            CHARACTER_ENCODING = 347,
            ROOT_NAME = 348,
            USED_DISK_SIZE = 349,
            VOLUME_PATH = 350,
            MAX_CHILD_DEPTH = 351,
            TOTAL_CHILD_SIZE = 352,
            ROW = 353,
            CHILD_OCCURRENCE_COUNT = 354,
            VOLUME_NAME = 355,
            DESCENDANT_OCCURRENCE_COUNT = 356,
            OBJECT_ID = 357,
            BIRTH_VOLUME_ID = 358,
            BIRTH_OBJECT_ID = 359,
            DOMAIN_ID = 360,
            FOLDER_DATA_CRC32 = 361,
            FOLDER_DATA_CRC64 = 362,
            FOLDER_DATA_MD5 = 363,
            FOLDER_DATA_SHA1 = 364,
            FOLDER_DATA_SHA256 = 365,
            FOLDER_DATA_SHA512 = 366,
            FOLDER_DATA_AND_NAMES_CRC32 = 367,
            FOLDER_DATA_AND_NAMES_CRC64 = 368,
            FOLDER_DATA_AND_NAMES_MD5 = 369,
            FOLDER_DATA_AND_NAMES_SHA1 = 370,
            FOLDER_DATA_AND_NAMES_SHA256 = 371,
            FOLDER_DATA_AND_NAMES_SHA512 = 372,
            FOLDER_NAMES_CRC32 = 373,
            FOLDER_NAMES_CRC64 = 374,
            FOLDER_NAMES_MD5 = 375,
            FOLDER_NAMES_SHA1 = 376,
            FOLDER_NAMES_SHA256 = 377,
            FOLDER_NAMES_SHA512 = 378,
            FOLDER_DATA_CRC32_FROM_DISK = 379,
            FOLDER_DATA_CRC64_FROM_DISK = 380,
            FOLDER_DATA_MD5_FROM_DISK = 381,
            FOLDER_DATA_SHA1_FROM_DISK = 382,
            FOLDER_DATA_SHA256_FROM_DISK = 383,
            FOLDER_DATA_SHA512_FROM_DISK = 384,
            FOLDER_DATA_AND_NAMES_CRC32_FROM_DISK = 385,
            FOLDER_DATA_AND_NAMES_CRC64_FROM_DISK = 386,
            FOLDER_DATA_AND_NAMES_MD5_FROM_DISK = 387,
            FOLDER_DATA_AND_NAMES_SHA1_FROM_DISK = 388,
            FOLDER_DATA_AND_NAMES_SHA256_FROM_DISK = 389,
            FOLDER_DATA_AND_NAMES_SHA512_FROM_DISK = 390,
            FOLDER_NAMES_CRC32_FROM_DISK = 391,
            FOLDER_NAMES_CRC64_FROM_DISK = 392,
            FOLDER_NAMES_MD5_FROM_DISK = 393,
            FOLDER_NAMES_SHA1_FROM_DISK = 394,
            FOLDER_NAMES_SHA256_FROM_DISK = 395,
            FOLDER_NAMES_SHA512_FROM_DISK = 396,
            LONG_NAME = 397,
            LONG_FULL_PATH = 398,
            DIGITAL_SIGNATURE_NAME = 399,
            DIGITAL_SIGNATURE_TIMESTAMP = 400,
            AUDIO_TRACK_COUNT = 401,
            VIDEO_TRACK_COUNT = 402,
            SUBTITLE_TRACK_COUNT = 403,
            NETWORK_INDEX_HOST = 404,
            ORIGINAL_LOCATION = 405,
            DATE_DELETED = 406,
            STATUS = 407,
            VORBIS_COMMENT = 408,
            QUICKTIME_METADATA = 409,
            PARENT_SIZE = 410,
            ROOT_SIZE = 411,
            OPENS_WITH = 412,
            RANDOMIZE = 413,
            ICON = 414, 
            THUMBNAIL = 415,
            CONTENT = 416,
            SEPARATOR = 417
        }
        #endregion
        internal const string dllName = "Everything3_x64.dll";

        // Connect to Everything
        // instance name can be NULL or an empty string to connect to the main unnamed instance.
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern IntPtr Everything3_ConnectW(string instance_name);

        // Destroy an Everything client.
        // disconnects from everything and frees any resources back to the system.
        [DllImport(dllName)]
        internal static extern bool Everything3_DestroyClient(IntPtr client);

        // general
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern uint Everything3_IncRunCountFromFilenameW(IntPtr client, string filename);
        [DllImport(dllName)]
        internal static extern uint Everything3_GetLastError();
        internal static string ECP_TranslateError(uint value)
        {
            return value switch
            {
                0 => Resources.EVERYTHING3_OK,
                0xE0000001 => Resources.EVERYTHING3_ERROR_OUT_OF_MEMORY,
                0xE0000002 => Resources.EVERYTHING3_ERROR_IPC_PIPE_NOT_FOUND,
                0xE0000003 => Resources.EVERYTHING3_ERROR_DISCONNECTED,
                0xE0000004 => Resources.EVERYTHING3_ERROR_INVALID_PARAMETER,
                0xE0000005 => Resources.EVERYTHING3_ERROR_BAD_REQUEST,
                0xE0000006 => Resources.EVERYTHING3_ERROR_CANCELLED,
                0xE0000007 => Resources.EVERYTHING3_ERROR_PROPERTY_NOT_FOUND,
                0xE0000008 => Resources.EVERYTHING3_ERROR_SERVER,
                0xE0000009 => Resources.EVERYTHING3_ERROR_INVALID_COMMAND,
                0xE000000A => Resources.EVERYTHING3_ERROR_BAD_RESPONSE,
                0xE000000B => Resources.EVERYTHING3_ERROR_INSUFFICIENT_BUFFER,
                0xE000000C => Resources.EVERYTHING3_ERROR_SHUTDOWN,
                0xE000000D => Resources.EVERYTHING3_ERROR_INVALID_PROPERTY_VALUE_TYPE,
                _ => Resources.ERROR_UNKNOWN,
            };
        }

        // Setup the search state.
        [DllImport(dllName)]
        internal static extern IntPtr Everything3_CreateSearchState();
        [DllImport(dllName)]
        internal static extern bool Everything3_DestroySearchState(IntPtr search_state);
        [DllImport(dllName)]
        internal static extern bool Everything3_SetSearchMatchPath(IntPtr search_state, bool match_path);
        [DllImport(dllName)]
        internal static extern bool Everything3_SetSearchRegex(IntPtr search_state, bool match_regex);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern bool Everything3_SetSearchTextW(IntPtr search_state, string search);
        [DllImport(dllName)]
        internal static extern bool Everything3_AddSearchSort(IntPtr search_state, Property sort, bool ascending);
        [DllImport(dllName)]
        internal static extern bool Everything3_ClearSearchSorts(IntPtr search_state);
        [DllImport(dllName)]
        internal static extern bool Everything3_AddSearchPropertyRequest(IntPtr search_state, Property property);
        [DllImport(dllName)]
        internal static extern bool Everything3_ClearSearchPropertyRequests(IntPtr search_state);

        // execute a search
        [DllImport(dllName)]
        internal static extern IntPtr Everything3_Search(IntPtr client, IntPtr search_state);
        [DllImport(dllName)]
        internal static extern IntPtr Everything3_GetResults(IntPtr client, IntPtr search_state);
        [DllImport(dllName)]
        internal static extern IntPtr Everything3_Sort(IntPtr client, IntPtr search_state);
        [DllImport(dllName)]
        internal static extern bool Everything3_DestroyResultList(IntPtr result_list);

        // Result list.
        [DllImport(dllName)]
        internal static extern uint Everything3_GetResultListCount(IntPtr result_list);
        [DllImport(dllName)]
        internal static extern bool Everything3_IsFolderResult(IntPtr result_list, uint result_index);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern ulong Everything3_GetResultFullPathNameW(IntPtr result_list, uint result_index, [Out] char[] wbuf, uint wbuf_size_in_wchars);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern ulong Everything3_GetResultNameW(IntPtr result_list, uint result_index, [Out] char[] wbuf, uint wbuf_size_in_wchars);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern ulong Everything3_GetResultPathW(IntPtr result_list, uint result_index, [Out] char[] wbuf, uint wbuf_size_in_wchars);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        internal static extern ulong Everything3_GetResultExtensionW(IntPtr result_list, uint result_index, [Out] char[] wbuf, uint wbuf_size_in_wchars);
        [DllImport(dllName)]
        internal static extern ulong Everything3_GetResultPropertyUINT64(IntPtr result_list, uint result_index, Property property);
        internal static string ECP_GetModifiedDateTime(IntPtr result_list, uint result_index)
        {
            ulong ft = Everything3_GetResultPropertyUINT64(result_list, result_index, Property.DATE_MODIFIED);
            if (ft == ulong.MaxValue) return string.Empty;
            DateTime dt = DateTime.FromFileTimeUtc((long)ft).ToLocalTime();
            return dt.ToString("yyyy-MM-dd hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
        }

        [DllImport(dllName)]
        internal static extern ulong Everything3_GetResultSize(IntPtr result_list, uint result_index);
        internal static string ECP_GetSize(IntPtr result_list, uint result_index)
        {
            ulong b = Everything3_GetResultSize(result_list, result_index);
            float kb = b / 1024;
            if (kb == 0)
                return $"{b} Bytes";
            if (kb < 1000)
                return $"{kb:F2} KB";
            float mb = kb / 1024;
            if (mb < 1000)
                return $"{mb:F2} MB";
            float gb = mb / 1024;
            if (gb < 1000)
                return $"{gb:F2} GB";
            float tb = gb / 1024;
            return $"{tb:F2} TB";
        }
    }
}
