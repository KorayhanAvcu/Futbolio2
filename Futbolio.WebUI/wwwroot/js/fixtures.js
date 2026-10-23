
(function () {
    'use strict';

    // Index.cshtml'deki <script type="application/json"> etiketleri çalışmaz, sadece veri taşır.
    // Burada içlerindeki metni JSON.parse ile nesneye çeviriyoruz; bozuksa fallback dönüyor.
    const readJson = (id, fallback) => {
        try {
            return JSON.parse(document.getElementById(id)?.textContent || '') ?? fallback;
        } catch {
            return fallback;
        }
    };

    /* Takımlar ve haftalar: Index.cshtml'in API'den aldığı veriler (JSON) */
    const TEAMS = readJson('fixtures-teams', {});
    const WEEKS = Object.fromEntries(readJson('fixtures-weeks', []).map(w => [w.week, w]));
    const WEEK_KEYS = Object.keys(WEEKS).map(Number).sort((a, b) => a - b);

    const team = id => TEAMS[id] || {
        name: 'Bilinmeyen takım', code: '?', a: '#8A97AD', b: '#FFFFFF', ink: '#fff', stadium: '', logo: ''
    };

    /* API durum kodu -> sayfa anahtarı (0: Planlandı, 1: Canlı, 2: Tamamlandı) */
    const STATUS_KEY = { 0: 'NotStarted', 1: 'Live', 2: 'Finished' };

    const STATUS_LABEL = {
        NotStarted: 'Başlamadı',
        Live: 'Canlı',
        Finished: 'Maç bitti'
    };

    /* API maçını sayfanın kullandığı biçime çevir */
    const toFixture = match => {
        const status = STATUS_KEY[match.status] || 'NotStarted';

        return {
            id: match.id,
            kickoff: String(match.matchDate).slice(0, 16),
            home: match.homeTeamId,
            away: match.awayTeamId,
            status,
            stadium: match.stadium,
            score: status === 'NotStarted'
                ? null
                : { home: match.homeScore, away: match.awayScore }
        };
    };

    /* Haftanın maçları: WebUI controller -> API (GET /api/matches/week/{week}) */
    // Tarayıcı API'ye (7191) doğrudan gitmez: farklı port farklı site sayılır ve API'de CORS izni yok.
    // Bunun yerine WebUI'daki FixturesController.Week'e gider, o da API'yi çağırır.
    const FixtureService = {
        async getWeek(week) {
            const res = await fetch(`/Fixtures/Week/${week}`, {
                headers: { Accept: 'application/json' }
            });

            if (!res.ok) {
                throw new Error('HTTP ' + res.status);
            }

            return (await res.json()).map(toFixture);
        }
    };

    const toDate = value =>
        new Date(value.length === 10 ? value + 'T00:00' : value);

    const fmtDay = new Intl.DateTimeFormat('tr-TR', {
        day: 'numeric',
        month: 'long',
        weekday: 'long'
    });

    const fmtDayMonth = new Intl.DateTimeFormat('tr-TR', {
        day: 'numeric',
        month: 'long'
    });

    const fmtWeekday = new Intl.DateTimeFormat('tr-TR', {
        weekday: 'long'
    });

    const fmtTime = new Intl.DateTimeFormat('tr-TR', {
        hour: '2-digit',
        minute: '2-digit'
    });

    const dayKey = iso => iso.slice(0, 10);

    const esc = value =>
        String(value).replace(/[&<>"']/g, char => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        })[char]);

    function weekRange(week) {
        const current = WEEKS[week];
        const start = toDate(current.start);
        const end = toDate(current.end);

        return `${fmtDayMonth.format(start)} – ${fmtDayMonth.format(end)} ${end.getFullYear()}`;
    }

    function crest(teamId, mod = '') {
        const t = team(teamId);

        if (t.logo) {
            return `<img class="team-crest ${mod}" src="${esc(t.logo)}" alt="" aria-hidden="true">`;
        }

        return `<span class="team-crest ${mod}"
      style="--crest-a:${esc(t.a)};--crest-b:${esc(t.b)};--crest-ink:${esc(t.ink)}"
      aria-hidden="true">${esc(t.code)}</span>`;
    }

    const hasScore = fixture =>
        fixture.score !== null &&
        (fixture.status === 'Finished' || fixture.status === 'Live');

    const detailUrl = fixture => `/MatchDetail/Index/${fixture.id}`;

    const chevron = `
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"
      stroke-width="2.4" stroke-linecap="round"
      stroke-linejoin="round" aria-hidden="true">
      <path d="M9 6l6 6-6 6"/>
    </svg>`;

    function statusBadge(fixture) {
        const label = STATUS_LABEL[fixture.status];

        return `<span class="status status--${fixture.status}">${label}</span>`;
    }

    /* Haftalık istatistikler */
    function computeSummary(list) {
        const played = list.filter(hasScore);
        const finished = list.filter(f => f.status === 'Finished');

        const goals = played.reduce(
            (sum, fixture) => sum + fixture.score.home + fixture.score.away,
            0
        );

        return {
            total: list.length,
            finished: finished.length,
            live: list.filter(f => f.status === 'Live').length,
            notStarted: list.filter(f => f.status === 'NotStarted').length,
            goals,
            avgGoals: played.length ? goals / played.length : 0,
            draws: finished.filter(f => f.score.home === f.score.away).length,
            homeWins: finished.filter(f => f.score.home > f.score.away).length,
            awayWins: finished.filter(f => f.score.home < f.score.away).length
        };
    }

    /* Maç kartı */
    function fixtureCard(fixture) {
        const home = team(fixture.home);
        const away = team(fixture.away);

        let homeCls = '';
        let awayCls = '';

        if (
            fixture.status === 'Finished' &&
            fixture.score &&
            fixture.score.home !== fixture.score.away
        ) {
            const homeWon = fixture.score.home > fixture.score.away;
            homeCls = homeWon ? 'is-winner' : 'is-loser';
            awayCls = homeWon ? 'is-loser' : 'is-winner';
        }

        const score = hasScore(fixture)
            ? `<div class="fx-score"
          aria-label="Skor ${fixture.score.home}-${fixture.score.away}">
          <span>${fixture.score.home}</span>
          <span class="fx-score__sep" aria-hidden="true"></span>
          <span>${fixture.score.away}</span>
        </div>`
            : `<div class="fx-score fx-score--empty" aria-label="Skor yok">-</div>`;

        let action = `<span>${esc(fixture.stadium || home.stadium)}</span>`;

        if (fixture.status === 'Finished') {
            action = `<a class="fx-link" href="${detailUrl(fixture)}">Maç detayı${chevron}</a>`;
        }

        if (fixture.status === 'Live') {
            action = `<a class="fx-link" href="${detailUrl(fixture)}">Canlı takip${chevron}</a>`;
        }

        const linked =
            fixture.status === 'Finished' || fixture.status === 'Live';

        return `
      <li>
        <article
          class="fixture-card fixture-card--${fixture.status}${linked ? ' is-linked' : ''}"
          data-match-id="${fixture.id}"
          data-match-status="${fixture.status}">

          <div class="fx-meta">
            <time class="fx-time" datetime="${fixture.kickoff}">
              ${fmtTime.format(toDate(fixture.kickoff))}
            </time>
            ${statusBadge(fixture)}
          </div>

          <div class="fx-team fx-team--home ${homeCls}">
            <span class="fx-name">${esc(home.name)}</span>
            ${crest(fixture.home)}
          </div>

          ${score}

          <div class="fx-team fx-team--away ${awayCls}">
            ${crest(fixture.away)}
            <span class="fx-name">${esc(away.name)}</span>
          </div>

          <div class="fx-action">${action}</div>
        </article>
      </li>`;
    }

    /* Maçları günlere göre grupla */
    function renderFixtures(list) {
        const groups = new Map();

        list.slice()
            .sort((a, b) =>
                a.kickoff.localeCompare(b.kickoff) || a.id - b.id
            )
            .forEach(fixture => {
                const key = dayKey(fixture.kickoff);

                if (!groups.has(key)) {
                    groups.set(key, []);
                }

                groups.get(key).push(fixture);
            });

        if (!list.length) {
            els.list.innerHTML =
                '<p class="page-note">Bu hafta için henüz fikstür yayımlanmadı.</p>';
            return;
        }

        els.list.innerHTML = Array.from(groups.entries())
            .map(([key, items]) => `
        <section class="day-group" aria-label="${fmtDay.format(toDate(key))}">
          <div class="day-head">
            <h3 class="day-head__date">${fmtDay.format(toDate(key))}</h3>
            <span class="day-head__count">${items.length} maç</span>
          </div>
          <ul class="day-list">${items.map(fixtureCard).join('')}</ul>
        </section>`)
            .join('');
    }

    /* Hafta seçiciyi oluştur */
    function renderWeekSelector() {
        const current = Number(els.page.dataset.currentWeek);

        els.weekList.innerHTML = WEEK_KEYS
            .map(week => `
        <li>
          <button type="button"
            class="week-btn"
            data-week="${week}"
            aria-pressed="false">

            <span class="week-btn__top">
              <span class="week-btn__title">${week}. Hafta</span>

              ${Number(week) === current
                    ? '<span class="week-tag week-tag--current">Güncel</span>'
                    : ''}

              <span class="week-tag week-tag--active">Aktif hafta</span>
            </span>

            <span class="week-btn__range">${weekRange(week)}</span>
          </button>
        </li>`)
            .join('');
    }

    function syncWeekSelector(week) {
        els.weekList.querySelectorAll('[data-week]').forEach(button => {
            button.setAttribute(
                'aria-pressed',
                String(Number(button.dataset.week) === week)
            );
        });

        els.prev.disabled = week <= WEEK_KEYS[0];
        els.next.disabled = week >= WEEK_KEYS[WEEK_KEYS.length - 1];
    }

    /* Aktif hafta bilgisi */
    function renderWeekInfo(week, list, summary) {
        const ordered = list.slice().sort((a, b) =>
            a.kickoff.localeCompare(b.kickoff) || a.id - b.id
        );

        const doneText = summary.finished === summary.total
            ? 'Hafta tamamlandı'
            : `${summary.finished} / ${summary.total} maç tamamlandı`;

        els.info.innerHTML = `
      <div>
        <h2 class="week-info__title">${week}. Hafta</h2>
        <p class="week-info__range">${weekRange(week)}</p>
      </div>

      <ul class="week-facts">
        <li><strong>${summary.total}</strong>maç</li>
        <li><strong>${list.length * 2}</strong>takım</li>
        <li><strong>${summary.goals}</strong>gol</li>
        ${summary.live ? `<li><strong>${summary.live}</strong>canlı</li>` : ''}
      </ul>

      <div class="week-progress">
        <div class="week-progress__head">
          <span>Hafta ilerlemesi</span>
          <strong>${doneText}</strong>
        </div>

        <div class="week-progress__bar" aria-hidden="true">
          ${ordered.map(fixture => `
            <span class="seg seg--${fixture.status}"
              title="${esc(team(fixture.home).name)} - ${esc(team(fixture.away).name)}: ${STATUS_LABEL[fixture.status]}">
            </span>`).join('')}
        </div>

        <div class="week-progress__legend" aria-hidden="true">
          <span><i style="background:rgba(31,201,138,.6)"></i>Bitti</span>
          <span><i style="background:var(--green)"></i>Canlı</span>
          <span><i></i>Başlamadı</span>
        </div>
      </div>`;
    }

    /* Haftalık özet */
    function renderSummary(week, summary) {
        const decided = summary.homeWins + summary.draws + summary.awayWins;
        const average = summary.avgGoals.toFixed(2).replace('.', ',');

        els.summary.innerHTML = `
      <div class="panel__head">
        <h2 class="panel__title" id="summary-title">${week}. Hafta özeti</h2>
      </div>

      <dl class="summary-list">
        <div><dt>Maç</dt><dd>${summary.total}</dd></div>
        <div><dt>Tamamlanan</dt><dd>${summary.finished}</dd></div>
        <div><dt>Gol</dt><dd>${summary.goals}</dd></div>
        <div><dt>Maç başına gol</dt><dd>${average}</dd></div>
        <div><dt>Beraberlik</dt><dd>${summary.draws}</dd></div>
        <div><dt>Ev sahibi galibiyeti</dt><dd>${summary.homeWins}</dd></div>
        <div><dt>Deplasman galibiyeti</dt><dd>${summary.awayWins}</dd></div>
      </dl>

      <div class="result-split">
        <span class="result-split__title">Sonuç dağılımı</span>

        ${decided ? `
          <div class="result-split__bar" aria-hidden="true">
            <span class="r-home" style="flex:${summary.homeWins}"></span>
            <span class="r-draw" style="flex:${summary.draws}"></span>
            <span class="r-away" style="flex:${summary.awayWins}"></span>
          </div>

          <div class="result-split__legend">
            <span><i style="background:var(--blue)"></i>Ev ${summary.homeWins}</span>
            <span><i style="background:var(--dim)"></i>Beraberlik ${summary.draws}</span>
            <span><i style="background:var(--ice)"></i>Deplasman ${summary.awayWins}</span>
          </div>
        ` : `
          <div class="result-split__legend">
            <span>Henüz tamamlanan maç yok</span>
          </div>
        `}
      </div>

      <p class="summary-note">
        Gol sayısına canlı maçlar dahildir, sonuçlar yalnızca biten maçlardan hesaplanır.
      </p>`;
    }

    /* Haftanın öne çıkan maçı: canlı maç, yoksa en gollü biten maç, yoksa ilk maç */
    function pickFeatured(list) {
        const goals = f => f.score ? f.score.home + f.score.away : -1;

        return list.find(f => f.status === 'Live') ||
            list.filter(f => f.status === 'Finished').sort((a, b) => goals(b) - goals(a))[0] ||
            list.slice().sort((a, b) => a.kickoff.localeCompare(b.kickoff))[0];
    }

    function renderFeatured(week, list) {
        const fixture = pickFeatured(list);

        if (!fixture) {
            els.featured.innerHTML = '';
            return;
        }

        const home = team(fixture.home);
        const away = team(fixture.away);
        const date = toDate(fixture.kickoff);

        const center = hasScore(fixture)
            ? `<span class="featured__score">${fixture.score.home}-${fixture.score.away}</span>`
            : '<span class="featured__score featured__score--vs">vs</span>';

        const link =
            fixture.status === 'Finished' || fixture.status === 'Live'
                ? `<a class="fx-link" href="${detailUrl(fixture)}">Maç detayı${chevron}</a>`
                : '';

        els.featured.innerHTML = `
      <div class="panel__head">
        <h2 class="panel__title" id="featured-title">Haftanın öne çıkan maçı</h2>
      </div>

      <div class="featured__body">
        <div class="featured__teams">
          <div class="featured__team">
            ${crest(fixture.home, 'team-crest--lg')}
            <span>${esc(home.name)}</span>
          </div>

          ${center}

          <div class="featured__team">
            ${crest(fixture.away, 'team-crest--lg')}
            <span>${esc(away.name)}</span>
          </div>
        </div>

        <ul class="featured__info">
          <li>${esc(fixture.stadium || home.stadium)}</li>
          <li>
            <span>${fmtWeekday.format(date)}, ${fmtDayMonth.format(date)}</span>
            ${fmtTime.format(date)}
          </li>
        </ul>

        <div class="featured__foot">
          ${statusBadge(fixture)}
          ${link}
        </div>
      </div>`;
    }

    /* HTML elemanları */
    const els = {
        page: document.querySelector('[data-fixtures-page]'),
        weekList: document.querySelector('[data-week-list]'),
        prev: document.querySelector('[data-week-step="-1"]'),
        next: document.querySelector('[data-week-step="1"]'),
        info: document.querySelector('[data-week-info]'),
        list: document.querySelector('[data-fixtures-list]'),
        summary: document.querySelector('[data-weekly-summary]'),
        featured: document.querySelector('[data-featured-match]')
    };

    /* Script diğer sayfalarda yüklense bile hata oluşmasını önle */
    if (!els.page || !els.weekList || !els.prev || !els.next ||
        !els.info || !els.list || !els.summary || !els.featured) {
        return;
    }

    let activeWeek = null;
    let requestId = 0;

    const weekAt = step => WEEK_KEYS[WEEK_KEYS.indexOf(activeWeek) + step];

    async function loadWeek(week) {
        if (!WEEKS[week] || week === activeWeek) {
            return;
        }

        activeWeek = week;
        syncWeekSelector(week);
        els.list.classList.add('is-loading');

        const myRequest = ++requestId;

        try {
            const list = await FixtureService.getWeek(week);

            if (myRequest !== requestId) {
                return;
            }

            const summary = computeSummary(list);

            renderWeekInfo(week, list, summary);
            renderFixtures(list);
            renderSummary(week, summary);
            renderFeatured(week, list);

            try {
                history.replaceState(null, '', `?hafta=${week}`);
            } catch {
                // Tarayıcı geçmişi güncellenemiyorsa sayfa yine çalışabilir.
            }
        } catch (error) {
            console.error('Fikstür yüklenemedi:', error);
            els.list.innerHTML =
                '<p class="page-note">Fikstür yüklenirken bir hata oluştu.</p>';
        } finally {
            if (myRequest === requestId) {
                els.list.classList.remove('is-loading');
            }
        }
    }

    /* Hafta düğmeleri */
    els.weekList.addEventListener('click', event => {
        const button = event.target.closest('[data-week]');

        if (button) {
            loadWeek(Number(button.dataset.week));
        }
    });

    /* Klavye ile hafta değiştirme */
    els.weekList.addEventListener('keydown', event => {
        if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') {
            return;
        }

        const step = event.key === 'ArrowLeft' ? -1 : 1;
        const target = weekAt(step);

        if (!WEEKS[target]) {
            return;
        }

        loadWeek(target);

        els.weekList
            .querySelector(`[data-week="${target}"]`)
            ?.focus();
    });

    /* Önceki ve sonraki hafta */
    [els.prev, els.next].forEach(button => {
        button.addEventListener('click', () => {
            const target = weekAt(Number(button.dataset.weekStep));

            if (target !== undefined) {
                loadWeek(target);
            }
        });
    });

    /* Kartın boş alanına tıklayınca maç detayını aç */
    els.list.addEventListener('click', event => {
        if (event.target.closest('a')) {
            return;
        }

        const card = event.target.closest('.fixture-card.is-linked');
        const link = card?.querySelector('.fx-link');

        if (link) {
            link.click();
        }
    });

    /* Başlangıç: URL'deki haftayı veya varsayılan haftayı aç */
    renderWeekSelector();

    if (!WEEK_KEYS.length) {
        els.prev.disabled = true;
        els.next.disabled = true;
        els.list.innerHTML = '<p class="page-note">Gösterilecek maç yok.</p>';
        return;
    }

    const fromUrl = Number(
        new URLSearchParams(location.search).get('hafta')
    );

    loadWeek(
        WEEKS[fromUrl]
            ? fromUrl
            : Number(els.page.dataset.currentWeek)
    );
})();
