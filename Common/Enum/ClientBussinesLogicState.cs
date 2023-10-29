namespace Common.Enum
{
    public enum ClientBussinesLogicState
    {
        NONE,
        REQUESTING_FILE,
        REQUEST_SENDED,
        REQUEST_ACCEPTED,
        WAITING_FOR_FILE_PART,
        WAITING_FOR_RESPONSE_TO_REQUEST,
        OFFERING_FILES_RECEIVING,
        OFFERING_FILES_SENDING,
        NODE_LIST_RECEIVING
    }
}
