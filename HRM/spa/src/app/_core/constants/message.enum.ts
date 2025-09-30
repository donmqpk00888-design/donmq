export enum MessageConstants {
  LOGGED_IN = 'Welcome to the system',
  LOGIN_FAILED = 'Your username and/or password do not match',
  CONFIRM_PASSWORD = 'New Password and Confirm Password do not match',

  LOGGED_OUT = 'You have been logged out',
  NEW_VERSION_AVAILABLE = 'New Version Available! Try re-login again',

  CONFIRM_DELETE = 'Are you sure delete?',
  CONFIRM_DELETE_MSG = 'Are you sure you want to delete this record?',
  CONFIRM_DELETE_RANGE_MSG = 'Are you sure you want to delete these records?',
  CONFIRM_CHANGE_STATE = 'Are you sure you want to change this state?',
  CONFIRM_CHANGE_YEAR = 'Current task year is difference from expected start year, do you want to change task year?',
  CONFIRM_LOGOUT_CHANGE_USER = 'Current user was update. Try re-login again?',

  CREATED_OK_MSG = 'Create successfully',
  UPDATED_OK_MSG = 'Update successfully',
  DELETED_OK_MSG = 'Delete successfully',
  UPLOADED_OK_MSG = 'Uploaded successfully',
  CHANGE_PASSWORD_OK_MSG = 'Change Password successfully',
  SENDED_OK_MSG = 'Send Mail successfully',

  CREATED_ERROR_MSG = 'Creating failed on save',
  UPDATED_ERROR_MSG = 'Updating failed on save',
  DELETED_ERROR_MSG = 'Deleting failed on save',
  UPLOAD_ERROR_MSG = 'Uploading failed on save',
  CHANGE_PASSWORD_ERROR_MSG = 'Change Password failed on save',
  SENDED_ERROR_MSG = 'Send Mail failed on save',

  UNAUTHORIZED = 'You are not authorized to view this page',

  UN_KNOWN_ERROR = 'Oops! Sorry, an error occurred while processing your request',
  SYSTEM_ERROR_MSG = 'An error occurred while connecting to the server',
  SESSION_EXPIRED = "Your session has expired, please log in again.",
  TRANSACTION_ERROR_MSG = 'A database transaction is already active. Please try againt later',

  NO_DATA = 'No Data!',
  DATA_FOUND = 'Data Not Found!',

  INVALID_FILE = 'Please select a file',
  INVALID_VALUE = 'Invalid value. Please check again',
  ALLOW_IMAGE_FILE = 'Allowed file extensions are .jpg, .png, .jpeg',
  ALLOW_VIDEO_FILE = 'Allowed file extensions are .mp4',
  FILE_IMAGE_SIZE = 'File size must be 5MB or smaller',
  FILE_VIDEO_SIZE = 'File size must be 20MB or smaller',

  SELECT_DATE = 'Please select From Date and To Date',
  SELECT_FORMAT_DATE = 'Please enter the date in format "YYYY/MM/DD"',
  COMPARE_DATE = 'From Date cannot be greater than To Date',

  SELECT_RECORD = 'Please select at least 1 record',

  ALLOW_EXCEL_FILE = 'Please select the Excel file format',

  QUERY_SUCCESS = 'Query successfully',
  SELECT_ALL_QUERY_OPTION = 'Please select all query options',
  SELECT_QUERY_OPTIONS = 'Please select the necessary options (*) to query',

  CHECK_NOTIFICATION_SUCCESS = 'Thanks you for check message 😃',
  CHANGE_CONTENT = "Please change the content",

  CLEAR = 'Clear successfully',
}

export enum CaptionConstants {
  LOGIN_FAILED = 'Login Failed!',
  SUCCESS = 'Success!',
  ERROR = 'Error!',
  WARNING = 'Warning!',
  UNAUTHORIZED = 'Unauthorized!',
  INVALID = 'Invalid!',
  CHANGE_STATE = 'Change State',
  CHANGE_YEAR = 'Change Year',
}
