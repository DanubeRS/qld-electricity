import * as React from 'react';
import * as ReactDOM from 'react-dom';
import {Provider} from "react-redux";
import reducer from './store'
import DateRefresher from './components/dateRefresherComponent'
import GraphComponent from './components/graph'
import {applyMiddleware, createStore} from "redux";
import * as promiseMiddleware from 'redux-promise';

const store = createStore(reducer, applyMiddleware(promiseMiddleware));

ReactDOM.render(
    <Provider store={store}>
        <div>
            <DateRefresher></DateRefresher>
            <GraphComponent></GraphComponent>
        </div>
    </Provider>,
    document.getElementById("root")
)