namespace AIAdventure.Blackjack
{
    /// <summary>
    /// Kurpiyerin beden dili. Blackjack masası kurpiyerin nasıl canlandırıldığını
    /// (Animator, klipler) bilmez; sadece bu arayüz üzerinden "kart dağıt" der.
    /// </summary>
    public interface IDealerBody
    {
        /// <summary>Dağıtma hareketi başladıktan kaç saniye sonra kart elden çıkar.</summary>
        float ReleaseDelay { get; }

        /// <summary>Tek bir kart dağıtma hareketini oynatır.</summary>
        void PlayDeal();
    }
}
