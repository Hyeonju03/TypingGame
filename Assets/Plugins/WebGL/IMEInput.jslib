mergeInto(LibraryManager.library, (function () {
  var el;

  function ensureHiddenInput() {
    if (typeof document === 'undefined') return null;
    if (el) return el;
    el = document.getElementById('ime-input');
    if (!el) {
      el = document.createElement('input');
      el.type = 'text';
      el.id = 'ime-input';
      Object.assign(el.style, {
        position:'fixed', left:'-9999px', top:'-9999px',
        opacity:0, pointerEvents:'none'
      });
      document.body.appendChild(el);

      // 모든 입력 변화 → Unity로 전달
      el.addEventListener('input', function () {
        SendMessage('InputManager', 'ReceiveInputFromWeb', el.value);
      });

      // 엔터 입력 → 제출
      el.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
          SendMessage('InputManager', 'SubmitInputFromWeb', el.value);
          e.preventDefault();
        }
      });

      // 한글 조합 종료 동기화
      el.addEventListener('compositionend', function () {
        SendMessage('InputManager', 'ReceiveInputFromWeb', el.value);
      });
    }
    return el;
  }

  return {
    FocusExternalInput: function () {
      var node = ensureHiddenInput();
      if (node) node.focus();
    }
  };
})());
