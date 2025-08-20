mergeInto(LibraryManager.library, {
    // C#에서 DllImport("__Internal")로 호출할 함수
    FocusExternalInput: function() {
        var input = document.getElementById('external-input');
        if (input) input.focus();
    },

    // Unity로부터 한영 전환 요청 시
    OnLangKeyPressed: function() {
        console.log("한영키 감지됨 (JS)");
        // 필요하면 여기서 추가 처리 가능
    }
});
