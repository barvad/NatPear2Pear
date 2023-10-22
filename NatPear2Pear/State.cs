namespace NatPear2Pear
{
    public enum State
    {
        /// <summary>
        ///     Новый peer
        /// </summary>
        New,

        /// <summary>
        ///     Запрос на подключение к пиру отправлен на хаб
        /// </summary>
        ConnectRequestSendedToHub,

        /// <summary>
        ///     Ответ хаба (на запрос подключения к пиру) получен
        /// </summary>
        ResponseReceivedFromHub,
        RegisterRequestSendedToHub,
        /// <summary>
        ///     Зарегистрирован на хабе для ожидания запроса на подключение
        /// </summary>
        RegisteredOnHub,
        ConnectRequestAccepted,
        Connected,

    }
}