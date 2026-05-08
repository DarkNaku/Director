namespace DarkNaku.Director {
    /// <summary>
    /// 씬 로딩 진행 상황을 수신하기 위한 인터페이스.
    /// </summary>
    public interface ILoadingProgress {
        /// <summary>
        /// 로딩 진행률이 갱신될 때 호출됩니다.
        /// </summary>
        /// <param name="progress">0~1 범위의 정규화된 진행률.</param>
        void OnProgress(float progress);
    }
}
