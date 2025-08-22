mergeInto(LibraryManager.library, {
  InitExternalInput: function () {
    if (typeof document === 'undefined') return;

    var id = 'ime-input';
    var el = document.getElementById(id);
    if (!el) {
      el = document.createElement('input');
      el.id = id;
      el.type = 'text';
      el.autocomplete = 'off';
      el.autocapitalize = 'none';
      el.spellcheck = false;

      // IME는 display:none 금지
      el.style.position = 'fixed';
      el.style.left = '0';
      el.style.top  = '0';
      el.style.width = '1px';
      el.style.height = '1px';
      el.style.opacity = 0;
      el.style.pointerEvents = 'none';
      document.body.appendChild(el);

      // 상태
      el.dataset.buf  = '';  // 확정된 문자열
      el.dataset.comp = '0'; // 1: 조합중

      // ===== 성능: 프레임당 1회만 동기화 =====
      var lastSent = '';
      var dirty = false;
      function sendToUnity(method, payload){
        try{ if(window.unityInstance) window.unityInstance.SendMessage('InputManager', method, payload||''); }
        catch(e){ console.error('[IMEInput] SendMessage failed:', e); }
      }
      function liveText(){
        return (el.dataset.comp === '1') ? (el.dataset.buf + el.value) : el.dataset.buf;
      }
      function scheduleSync(){
        if (dirty) return;
        dirty = true;
        (window.requestAnimationFrame || function(cb){setTimeout(cb,16);})(function(){
          var s = liveText();
          if (s !== lastSent) { sendToUnity('ReceiveInputFromWeb', s); lastSent = s; }
          dirty = false;
        });
      }
      function delLast(s){ return Array.from(s).slice(0,-1).join(''); } // 유니코드 안전

      // ===== IME =====
      el.addEventListener('compositionstart', function(){ el.dataset.comp='1'; scheduleSync(); });
      el.addEventListener('compositionupdate', function(){ el.dataset.comp='1'; scheduleSync(); });
      el.addEventListener('compositionend', function(){
        el.dataset.comp='0';
        if (el.value){ el.dataset.buf = el.dataset.buf + el.value; el.value=''; }
        scheduleSync();
      });

      // ===== 일반 입력(영문/숫자/특수문자/스페이스) =====
      el.addEventListener('input', function(){
        if (el.dataset.comp === '1'){ scheduleSync(); return; } // 조합중엔 미리보기만
        if (el.value){ el.dataset.buf = el.dataset.buf + el.value; el.value=''; }
        scheduleSync();
      });

      // ===== 제어키 =====
      el.addEventListener('keydown', function(e){
        if (e.key === 'Enter'){
          e.preventDefault(); e.stopPropagation();
          if (el.dataset.comp === '1' && el.value){ // 조합중 강제 커밋
            el.dataset.buf = el.dataset.buf + el.value; el.value=''; el.dataset.comp='0';
          }
          var text = el.dataset.buf.trim();
          if (text.length > 0) sendToUnity('SubmitInputFromWeb', text);
          el.dataset.buf = '';
          scheduleSync(); // Unity 입력창 비우기
          el.focus({preventScroll:true});
          return;
        }
        if (e.key === 'Backspace'){
          if (el.dataset.comp === '1') return; // 조합중 백스페이스는 IME가 처리
          el.dataset.buf = delLast(el.dataset.buf);
          scheduleSync();
          e.preventDefault(); e.stopPropagation();
          return;
        }
      });
    }

    // ====== 캔버스 포커스 차단 + 항상 숨은 input으로 복귀 ======
    var canvas = document.getElementById('unity-canvas') || document.querySelector('canvas');
    if (canvas) {
      canvas.setAttribute('tabindex', '-1'); // 포커스 불가
      canvas.style.outline = 'none';
      var refocus = function(ev){
        if (ev && ev.preventDefault) ev.preventDefault(); // 캔버스 기본 포커스 방지
        // 전파는 막지 않아 Unity 입력 이벤트는 그대로 전달됨
        if (el) el.focus({preventScroll:true});
      };
      // 캡처 단계에서 먼저 실행해 기본 포커스만 막음
      canvas.addEventListener('pointerdown', refocus, true);
      canvas.addEventListener('mousedown',  refocus, true);
      canvas.addEventListener('touchstart', refocus, true);
    }

    // 실수로 blur되면 즉시 복구
    el.addEventListener('blur', function(){ setTimeout(function(){ el && el.focus({preventScroll:true}); }, 0); });

    // 초기 포커스
    el.value = '';
    el.focus({preventScroll:true});
  },

  FocusExternalInput: function () {
    if (typeof document === 'undefined') return;
    var el = document.getElementById('ime-input');
    if (el) el.focus({preventScroll:true});
  }
});
